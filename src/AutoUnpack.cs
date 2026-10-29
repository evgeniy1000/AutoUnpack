// AutoUnpack — распаковщик архивов двойным кликом.
// Двойной клик по архиву: распаковка в папку с тем же именем рядом с архивом, без вопросов.
//
// Распаковывает сам Проводник Windows (как «Извлечь всё»): на больших архивах показывается
// стандартное окно копирования с прогрессом. Форматы, которые Проводник не открывает,
// распаковываются встроенным tar.exe.
//
// Режимы запуска:
//   AutoUnpack.exe               — значок в трее (меню: уведомления, открывать папку, автозапуск)
//   AutoUnpack.exe /tray         — то же (так запускается вместе с Windows)
//   AutoUnpack.exe архив         — распаковать и открыть папку на переднем плане
//   Shift + двойной клик         — открыть архив стандартным просмотром Windows, без распаковки
//                                  (при «открывать одним щелчком» — выделить архив и Shift+Enter)
//   AutoUnpack.exe /view архив   — то же (пункт контекстного меню «Просмотреть архив»)
//   AutoUnpack.exe /extract архив — распаковать, Shift не учитывать
//   AutoUnpack.exe /stampicon    — служебный: установщик вшивает в exe системный значок архива
//
// Распаковку и открытие папки делает процесс, запущенный двойным кликом: у него есть право
// вывести окно на передний план. Трей только показывает уведомления.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

static class AutoUnpack
{
    public const string Title = "Распаковщик";
    const string RegSettings = @"Software\AutoUnpack";
    const string RegRun = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string RunValue = "AutoUnpack";

    static readonly string[] DoubleExt = {
        ".tar.gz", ".tar.bz2", ".tar.xz", ".tar.zst", ".tar.lz", ".tar.lzma", ".tar.z"
    };

    // Имена мьютекса и канала привязаны к пользователю и сеансу: разные пользователи
    // (быстрое переключение) не мешают друг другу и не могут подменить чужой трей.
    static string userSid;
    static string UserSid
    {
        get
        {
            if (userSid == null) userSid = WindowsIdentity.GetCurrent().User.Value;
            return userSid;
        }
    }
    static string MutexName { get { return @"Local\AutoUnpack.Tray." + UserSid; } }
    public static string PipeName
    {
        get { return "AutoUnpack." + UserSid + "." + Process.GetCurrentProcess().SessionId; }
    }

    // Системные программы — только по полному пути (защита от подмены через текущую папку)
    public static string SysExe(string name) { return Path.Combine(Environment.SystemDirectory, name); }
    static string ExplorerExe
    {
        get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe"); }
    }

    [STAThread]
    static int Main(string[] args)
    {
        // Shift проверяется первым делом: пока .NET запускается, клавишу обычно уже отпускают
        bool viewKeys = ShiftHeld();

        List<string> files = new List<string>();
        bool forceExtract = false;    // пункт контекстного меню «Распаковать»: Shift не учитывать
        bool forceView = false;       // пункт контекстного меню «Просмотреть архив»
        foreach (string a in args)
        {
            if (string.Equals(a, "/stampicon", StringComparison.OrdinalIgnoreCase))
                return SystemIcon.StampIntoSelf();
            if (string.Equals(a, "/extract", StringComparison.OrdinalIgnoreCase)) { forceExtract = true; continue; }
            if (string.Equals(a, "/view", StringComparison.OrdinalIgnoreCase)) { forceView = true; continue; }
            if (a.StartsWith("/")) continue;
            try { files.Add(Path.GetFullPath(a)); } catch { files.Add(a); }   // до смены рабочей папки
        }

        // Рабочая папка при двойном клике — папка с архивом. Уходим из неё, чтобы ничего
        // случайно не запустить и не загрузить оттуда.
        try { Environment.CurrentDirectory = Environment.SystemDirectory; } catch { }

        if (files.Count == 0)
        {
            RunTray();
            return 0;
        }

        // Shift зажат при открытии — не распаковываем, а открываем архив
        // стандартным просмотром Windows (как папку в Проводнике)
        // (в контекстное меню попадают и через Shift+ПКМ — там клавиши не означают «просмотр»)
        // Пункт «Просмотреть архив» работает при любом режиме Проводника — в том числе
        // при «открывать одним щелчком», где щелчки с Shift заняты выделением файлов.
        if (forceView || (!forceExtract && viewKeys))
        {
            foreach (string f in files) OpenInExplorer(Path.GetFullPath(f));
            return 0;
        }

        bool openFolder = GetSetting("OpenFolder", true);
        int rc = 0;
        foreach (string f in files)
        {
            string full = Path.GetFullPath(f);

            // Защита от двойной распаковки одного архива (например, случайный повторный двойной
            // клик): пока архив распаковывается, второй запуск для него ничего не делает.
            Mutex busy = null;
            try
            {
                bool first;
                busy = new Mutex(true, @"Local\AutoUnpack.Extract." + PathHash(full), out first);
                if (!first) { busy.Dispose(); continue; }
            }
            catch { busy = null; }

            try
            {
                string target = Unpack(full);
                // Папка открывается только после полного завершения распаковки
                if (openFolder) OpenFolder(target, true);
                NotifyTray("OK\t" + target);
            }
            catch (OperationCanceledException)
            {
                // Пользователь нажал «Отмена» в окне копирования — молча выходим
            }
            catch (Exception e)
            {
                rc = 1;
                string msg = e.Message.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");
                // Ошибка — всегда: уведомлением из трея, а если трея нет — окном
                if (!NotifyTray("ERR\t" + full + "\t" + msg))
                    MessageBox.Show("Не удалось распаковать:\n" + f + "\n\n" + e.Message,
                        Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (busy != null)
                {
                    try { busy.ReleaseMutex(); } catch { }
                    busy.Dispose();
                }
            }
        }
        return rc;
    }

    static string PathHash(string path)
    {
        using (System.Security.Cryptography.SHA256 sha = System.Security.Cryptography.SHA256.Create())
        {
            byte[] h = sha.ComputeHash(Encoding.UTF8.GetBytes(path.ToUpperInvariant()));
            return BitConverter.ToString(h, 0, 16).Replace("-", "");
        }
    }

    // ---------- Просмотр архива (Shift + двойной клик) ----------

    [DllImport("user32.dll")] static extern short GetAsyncKeyState(int vk);
    [DllImport("user32.dll")] static extern short GetKeyState(int vk);

    // Зажат ли Shift. Две проверки:
    // 1) физическое состояние клавиши сейчас;
    // 2) состояние клавиатуры в окне Проводника на момент последнего обработанного им
    //    события — то есть примерно на момент двойного клика или Enter, даже если Shift уже отпустили.
    static bool ShiftHeld()
    {
        const int VK_SHIFT = 0x10;
        if ((GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0) return true;
        try
        {
            IntPtr fg = GetForegroundWindow();
            uint fgThread = GetWindowThreadProcessId(fg, IntPtr.Zero);
            uint myThread = GetCurrentThreadId();
            if (fgThread != 0 && fgThread != myThread && AttachThreadInput(myThread, fgThread, true))
            {
                try { return (GetKeyState(VK_SHIFT) & 0x8000) != 0; }
                finally { AttachThreadInput(myThread, fgThread, false); }
            }
        }
        catch { }
        return false;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct SHELLEXECUTEINFO
    {
        public int cbSize;
        public uint fMask;
        public IntPtr hwnd;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpVerb;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpFile;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpParameters;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpDirectory;
        public int nShow;
        public IntPtr hInstApp;
        public IntPtr lpIDList;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpClass;
        public IntPtr hkeyClass;
        public uint dwHotKey;
        public IntPtr hIcon;
        public IntPtr hProcess;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO info);

    // Открывает архив системным обработчиком Windows (CompressedFolder для .zip и т.п.),
    // в обход назначенной «по умолчанию» программы — то есть в обычном окне Проводника.
    static void OpenInExplorer(string archive)
    {
        if (!File.Exists(archive)) return;
        AllowSetForegroundWindow(-1);
        string progId = ShellExtract.SystemProgId(archive);
        if (progId != null)
        {
            // Основной способ: открыть архив как папку новым окном Проводника.
            // Работает, потому что у типа файла есть CLSID системного обработчика.
            SHELLEXECUTEINFO sei = new SHELLEXECUTEINFO();
            sei.cbSize = Marshal.SizeOf(typeof(SHELLEXECUTEINFO));
            sei.fMask = 0x00000400;                        // SEE_MASK_FLAG_NO_UI
            sei.lpVerb = "explore";
            sei.lpFile = archive;
            sei.nShow = 1;                                 // SW_SHOWNORMAL
            if (ShellExecuteEx(ref sei)) return;

            // Запасной способ: «открыть» от имени системного типа файла
            sei = new SHELLEXECUTEINFO();
            sei.cbSize = Marshal.SizeOf(typeof(SHELLEXECUTEINFO));
            sei.fMask = 0x00000001 | 0x00000400;          // SEE_MASK_CLASSNAME | SEE_MASK_FLAG_NO_UI
            sei.lpVerb = "open";
            sei.lpFile = archive;
            sei.lpClass = progId;
            sei.nShow = 1;
            if (ShellExecuteEx(ref sei)) return;
        }
        // Встроенного просмотра для этого формата в Windows нет — предлагаем выбрать программу
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo(SysExe("rundll32.exe"), "shell32.dll,OpenAs_RunDLL " + archive);
            psi.UseShellExecute = false;
            Process.Start(psi);
        }
        catch { }
    }

    // ---------- Трей ----------

    static void RunTray()
    {
        bool created;
        using (Mutex mtx = new Mutex(true, MutexName, out created))
        {
            if (!created) return;               // значок уже есть
            CleanupOldFiles();
            // Непредвиденная ошибка не должна показывать пользователю отчёт о сбое .NET
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += delegate { };
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            new TrayApp();
            Application.Run();
        }
    }

    // Убрать хвосты от вшивания значка при установке (если удаление не удалось сразу)
    static void CleanupOldFiles()
    {
        try
        {
            string dir = Path.GetDirectoryName(Application.ExecutablePath);
            foreach (string f in Directory.GetFiles(dir, "AutoUnpack.exe.*.old"))
                try { File.Delete(f); } catch { }
        }
        catch { }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool GetNamedPipeServerProcessId(IntPtr pipe, out uint pid);

    // Передать сообщение значку в трее (для уведомления). false — трей не запущен.
    static bool NotifyTray(string line)
    {
        try
        {
            Mutex m;
            if (!Mutex.TryOpenExisting(MutexName, out m)) return false;
            m.Dispose();

            using (NamedPipeClientStream c = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
            {
                c.Connect(2000);

                // Проверяем, что на том конце именно наша программа, а не подмена
                uint pid;
                if (!GetNamedPipeServerProcessId(c.SafePipeHandle.DangerousGetHandle(), out pid)) return false;
                using (Process p = Process.GetProcessById((int)pid))
                {
                    if (!string.Equals(p.MainModule.FileName, Application.ExecutablePath,
                                       StringComparison.OrdinalIgnoreCase))
                        return false;
                }

                using (StreamWriter w = new StreamWriter(c, new UTF8Encoding(false)))
                    w.WriteLine(line);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    // ---------- Настройки ----------

    public static bool GetSetting(string name, bool def)
    {
        try
        {
            using (RegistryKey k = Registry.CurrentUser.OpenSubKey(RegSettings))
            {
                if (k == null) return def;
                object v = k.GetValue(name);
                return v == null ? def : Convert.ToInt32(v) != 0;
            }
        }
        catch { return def; }
    }

    public static void SetSetting(string name, bool value)
    {
        try
        {
            using (RegistryKey k = Registry.CurrentUser.CreateSubKey(RegSettings))
                k.SetValue(name, value ? 1 : 0, RegistryValueKind.DWord);
        }
        catch { }
    }

    public static bool IsAutostart()
    {
        try
        {
            using (RegistryKey k = Registry.CurrentUser.OpenSubKey(RegRun))
                return k != null && k.GetValue(RunValue) != null;
        }
        catch { return false; }
    }

    public static void SetAutostart(bool on)
    {
        try
        {
            using (RegistryKey k = Registry.CurrentUser.CreateSubKey(RegRun))
            {
                if (on) k.SetValue(RunValue, "\"" + Application.ExecutablePath + "\" /tray");
                else k.DeleteValue(RunValue, false);
            }
        }
        catch (Exception e)
        {
            MessageBox.Show("Не удалось изменить автозапуск:\n" + e.Message, Title,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ---------- Открытие папки поверх всех окон ----------

    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern bool BringWindowToTop(IntPtr h);
    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr h, int cmd);
    [DllImport("user32.dll")] static extern bool IsIconic(IntPtr h);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, IntPtr pid);
    [DllImport("user32.dll")] static extern bool AttachThreadInput(uint a, uint b, bool attach);
    [DllImport("user32.dll")] static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
    [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] static extern bool AllowSetForegroundWindow(int pid);

    // wait = true — дождаться, пока окно откроется и выйдет на передний план
    // (нужно, когда программа сразу после этого завершается)
    public static void OpenFolder(string path, bool wait)
    {
        // Открываем только существующие папки — ничего другого через Проводник не запускаем
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return;

        // Передаём Проводнику своё право выйти на передний план —
        // тогда новое окно сразу открывается поверх всех, а не сзади.
        AllowSetForegroundWindow(-1);   // ASFW_ANY
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo(ExplorerExe, Q(path));
            psi.UseShellExecute = false;
            Process.Start(psi);
        }
        catch { return; }

        Thread t = new Thread(delegate () { ActivateWhenOpened(path); });
        t.SetApartmentState(ApartmentState.STA);   // Shell.Application требует STA
        t.IsBackground = !wait;
        t.Start();
        if (wait) t.Join();
    }

    static void ActivateWhenOpened(string path)
    {
        try
        {
            for (int i = 0; i < 60; i++)                // до ~6 секунд
            {
                Thread.Sleep(100);
                IntPtr h = FindExplorerWindow(path);
                if (h != IntPtr.Zero)
                {
                    // Обычно окно уже впереди; страховка — только если Windows всё же не пустила
                    if (GetForegroundWindow() != h) BringToFront(h);
                    return;
                }
            }
        }
        catch { }
    }

    static IntPtr FindExplorerWindow(string path)
    {
        Type t = Type.GetTypeFromProgID("Shell.Application");
        if (t == null) return IntPtr.Zero;
        dynamic shell = Activator.CreateInstance(t);
        try
        {
            string want = path.TrimEnd('\\');
            foreach (dynamic w in shell.Windows())
            {
                try
                {
                    string p = w.Document.Folder.Self.Path;
                    if (string.Equals(p.TrimEnd('\\'), want, StringComparison.OrdinalIgnoreCase))
                        return new IntPtr(Convert.ToInt64(w.HWND));
                }
                catch { }
            }
        }
        finally
        {
            Marshal.ReleaseComObject(shell);
        }
        return IntPtr.Zero;
    }

    static void BringToFront(IntPtr h)
    {
        if (IsIconic(h)) ShowWindow(h, 9);              // SW_RESTORE
        if (SetForegroundWindow(h) && GetForegroundWindow() == h) return;

        // Windows не даёт фоновой программе перехватывать фокус — обходим штатными способами
        uint fgThread = GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero);
        uint myThread = GetCurrentThreadId();
        AttachThreadInput(myThread, fgThread, true);
        BringWindowToTop(h);
        SetForegroundWindow(h);
        AttachThreadInput(myThread, fgThread, false);
        if (GetForegroundWindow() == h) return;

        keybd_event(0x12, 0, 0, UIntPtr.Zero);          // короткое нажатие Alt снимает блокировку
        keybd_event(0x12, 0, 2, UIntPtr.Zero);
        SetForegroundWindow(h);
    }

    // ---------- Распаковка ----------

    static string BaseName(string path)
    {
        string name = Path.GetFileName(path);
        string lower = name.ToLowerInvariant();
        foreach (string e in DoubleExt)
            if (lower.EndsWith(e) && name.Length > e.Length)
                return name.Substring(0, name.Length - e.Length);
        return Path.GetFileNameWithoutExtension(name);
    }

    static bool IsEmptyDir(string dir)
    {
        return Directory.GetFileSystemEntries(dir).Length == 0;
    }

    static bool IsReparsePoint(string path)
    {
        try { return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0; }
        catch { return false; }
    }

    // Имя занято: файл, непустая папка или ссылка/точка соединения. В ссылку не распаковываем
    // никогда, даже в «пустую»: она может вести в чужую папку (например, «Автозагрузку»).
    static bool IsOccupied(string path)
    {
        if (File.Exists(path)) return true;
        if (!Directory.Exists(path)) return false;
        return IsReparsePoint(path) || !IsEmptyDir(path);
    }

    // Возвращает путь к папке, куда распакован архив
    public static string Unpack(string archive)
    {
        if (!File.Exists(archive))
            throw new FileNotFoundException("Файл не найден.");

        string dir = Path.GetDirectoryName(archive);
        string baseName = BaseName(archive).TrimEnd(' ', '.');
        if (baseName.Length == 0) baseName = "archive";

        // Папка с тем же именем. Если такая уже есть и в ней что-то лежит —
        // не смешиваем файлы, а берём «Имя (2)», «Имя (3)» и т.д.
        string target = Path.Combine(dir, baseName);
        int n = 2;
        while (IsOccupied(target))
        {
            target = Path.Combine(dir, baseName + " (" + n + ")");
            n++;
        }
        bool created = !Directory.Exists(target);
        Directory.CreateDirectory(target);
        // Повторная проверка: папку могли подменить ссылкой между проверкой и созданием
        if (IsReparsePoint(target))
            throw new IOException("Папка назначения оказалась символической ссылкой — распаковка отменена.");

        string err = "";

        // 1) Основной способ — сам Проводник (тот же механизм, что «Извлечь всё»).
        //    На больших архивах Windows сама показывает стандартное окно копирования с прогрессом.
        try
        {
            if (ShellExtract.Run(archive, target))
            {
                // Проводник переносит пометку «скачано из интернета» для .zip, но не во всех
                // версиях Windows — для .rar/.7z. Дублируем сами: повторная запись безвредна.
                ZoneMark.Propagate(archive, target);
                Flatten(target, baseName);
                return target;
            }
        }
        catch (OperationCanceledException)
        {
            // Отменили — убираем недораспакованную папку, если её создали мы
            try { if (created) Directory.Delete(target, true); } catch { }
            throw;
        }
        catch (Exception e)
        {
            err = "Проводник: " + e.Message + "\n";
        }

        // 2) Запасной способ — встроенный tar.exe (для форматов, которые Проводник не открывает).
        //    tar по умолчанию отказывается писать за пределы папки (пути с «..», абсолютные пути,
        //    запись через символические ссылки).
        int code = -1;
        string tar = SysExe("tar.exe");
        if (File.Exists(tar))
        {
            string tarErr;
            code = Run(tar, "-xf " + Q(archive) + " -C " + Q(target), null, out tarErr);
            err += tarErr;
        }
        else
            err += "В системе не найден tar.exe.";

        if (code != 0)
        {
            try { if (created && IsEmptyDir(target)) Directory.Delete(target); } catch { }
            string msg = (err ?? "").Trim();
            if (msg.Length == 0) msg = "Код ошибки: " + code;
            throw new Exception(msg);
        }

        // tar не переносит пометку «скачано из интернета» — делаем это сами,
        // чтобы защита Windows (SmartScreen, защищённый просмотр Office) продолжала работать.
        ZoneMark.Propagate(archive, target);

        Flatten(target, baseName);
        return target;
    }

    // Если внутри архива лежит одна папка с тем же именем — убираем лишнюю вложенность
    // (вместо «Фото\Фото\...» получится «Фото\...»).
    static void Flatten(string target, string baseName)
    {
        try
        {
            string[] entries = Directory.GetFileSystemEntries(target);
            if (entries.Length != 1 || !Directory.Exists(entries[0])) return;
            if (!string.Equals(Path.GetFileName(entries[0]), baseName, StringComparison.OrdinalIgnoreCase)) return;
            // Символическую ссылку/точку соединения не «распрямляем» — она может вести за пределы папки
            if ((File.GetAttributes(entries[0]) & FileAttributes.ReparsePoint) != 0) return;

            string tmp = Path.Combine(target, "~unpack_" + Guid.NewGuid().ToString("N"));
            Directory.Move(entries[0], tmp);
            foreach (string d in Directory.GetDirectories(tmp))
                Directory.Move(d, Path.Combine(target, Path.GetFileName(d)));
            foreach (string f in Directory.GetFiles(tmp))
                File.Move(f, Path.Combine(target, Path.GetFileName(f)));
            Directory.Delete(tmp);
        }
        catch { /* не критично — файлы уже распакованы */ }
    }

    static int Run(string exe, string arguments, Dictionary<string, string> env, out string stderr)
    {
        ProcessStartInfo psi = new ProcessStartInfo(exe, arguments);
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;
        psi.WorkingDirectory = Environment.SystemDirectory;
        psi.RedirectStandardError = true;
        psi.RedirectStandardOutput = true;
        if (env != null)
            foreach (KeyValuePair<string, string> kv in env) psi.EnvironmentVariables[kv.Key] = kv.Value;
        try
        {
            psi.StandardErrorEncoding = Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage);
            psi.StandardOutputEncoding = psi.StandardErrorEncoding;
        }
        catch { }

        using (Process p = Process.Start(psi))
        {
            p.OutputDataReceived += delegate { };
            p.BeginOutputReadLine();
            stderr = p.StandardError.ReadToEnd();
            p.WaitForExit();
            return p.ExitCode;
        }
    }

    // В Windows имена файлов не могут содержать кавычку ", поэтому такое экранирование надёжно
    public static string Q(string s) { return "\"" + s + "\""; }
}

// Значок в трее: меню с настройками и уведомления о распаковке
class TrayApp
{
    readonly NotifyIcon icon;
    readonly Control ui;
    readonly ToolStripMenuItem miOpen, miNotify, miAutostart;
    string lastFolder;

    public TrayApp()
    {
        ui = new Control();
        IntPtr forceHandle = ui.Handle;   // для передачи событий в поток интерфейса

        ToolStripMenuItem header = new ToolStripMenuItem("Распаковщик архивов");
        header.Enabled = false;
        header.Font = new Font(header.Font, FontStyle.Bold);

        miNotify = new ToolStripMenuItem("Уведомлять о распаковке");
        miNotify.CheckOnClick = true;
        miNotify.Checked = AutoUnpack.GetSetting("Notify", false);
        miNotify.CheckedChanged += delegate { AutoUnpack.SetSetting("Notify", miNotify.Checked); };

        miOpen = new ToolStripMenuItem("Открывать папку после распаковки");
        miOpen.CheckOnClick = true;
        miOpen.Checked = AutoUnpack.GetSetting("OpenFolder", true);
        miOpen.CheckedChanged += delegate { AutoUnpack.SetSetting("OpenFolder", miOpen.Checked); };

        miAutostart = new ToolStripMenuItem("Запускать вместе с Windows");
        miAutostart.CheckOnClick = true;
        miAutostart.Checked = AutoUnpack.IsAutostart();
        miAutostart.CheckedChanged += delegate { AutoUnpack.SetAutostart(miAutostart.Checked); };

        ToolStripMenuItem miExit = new ToolStripMenuItem("Выход");
        miExit.Click += delegate
        {
            icon.Visible = false;
            icon.Dispose();
            Application.Exit();
        };

        ContextMenuStrip menu = new ContextMenuStrip();
        menu.Items.AddRange(new ToolStripItem[] {
            header, new ToolStripSeparator(),
            miNotify, miOpen, miAutostart,
            new ToolStripSeparator(), miExit
        });
        menu.Opening += delegate { miAutostart.Checked = AutoUnpack.IsAutostart(); };

        icon = new NotifyIcon();
        icon.Icon = SystemIcon.Small();
        icon.Text = "Распаковщик архивов";
        icon.ContextMenuStrip = menu;
        icon.BalloonTipClicked += delegate { AutoUnpack.OpenFolder(lastFolder, false); };
        icon.Visible = true;

        Thread t = new Thread(PipeLoop);
        t.IsBackground = true;
        t.Start();
    }

    void PipeLoop()
    {
        // Писать в канал может только текущий пользователь
        PipeSecurity sec = new PipeSecurity();
        sec.AddAccessRule(new PipeAccessRule(WindowsIdentity.GetCurrent().User,
            PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance, AccessControlType.Allow));

        int failures = 0;
        while (true)
        {
            try
            {
                using (NamedPipeServerStream s = new NamedPipeServerStream(AutoUnpack.PipeName, PipeDirection.In, 1,
                           PipeTransmissionMode.Byte, PipeOptions.None, 0, 4096, sec))
                {
                    failures = 0;
                    s.WaitForConnection();
                    using (StreamReader r = new StreamReader(s, Encoding.UTF8))
                    {
                        string line;
                        int count = 0;
                        while ((line = r.ReadLine()) != null && count++ < 100)
                        {
                            // "OK\t<папка>"  или  "ERR\t<архив>\t<текст ошибки>"
                            string[] parts = line.Split('\t');
                            if (parts.Length >= 2 && parts[0] == "OK")
                                Report(null, parts[1], null);
                            else if (parts.Length >= 3 && parts[0] == "ERR")
                                Report(parts[1], null, parts[2]);
                        }
                    }
                }
            }
            catch
            {
                // Канал занят или ошибка — подождать; при постоянных сбоях реже повторять
                failures++;
                Thread.Sleep(failures < 10 ? 500 : 10000);
            }
        }
    }

    void Report(string archive, string target, string error)
    {
        ui.BeginInvoke((MethodInvoker)delegate
        {
            // Данные пришли из канала — некорректная строка не должна ронять трей
            try
            {
                // Ошибки показываются всегда, независимо от галочки «Уведомлять»
                if (error != null)
                {
                    string msg = SafeName(archive) + "\n" + error;
                    if (msg.Length > 250) msg = msg.Substring(0, 250) + "…";
                    icon.ShowBalloonTip(8000, "Не удалось распаковать", msg, ToolTipIcon.Error);
                    return;
                }
                lastFolder = target;
                if (miNotify.Checked)
                    icon.ShowBalloonTip(3000, "Распаковано", SafeName(target), ToolTipIcon.Info);
            }
            catch { }
        });
    }

    static string SafeName(string path)
    {
        if (string.IsNullOrEmpty(path)) return "";
        int i = path.TrimEnd('\\').LastIndexOf('\\');
        string name = i >= 0 ? path.TrimEnd('\\').Substring(i + 1) : path;
        return name.Length > 120 ? name.Substring(0, 120) + "…" : name;
    }
}

// Пометка «файл скачан из интернета» (поток Zone.Identifier). Переносится с архива
// на все извлечённые файлы, как это делает сам Проводник.
static class ZoneMark
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern SafeFileHandle CreateFile(string name, uint access, uint share, IntPtr sec,
                                            uint disposition, uint flags, IntPtr template);

    const uint GENERIC_READ = 0x80000000, GENERIC_WRITE = 0x40000000;
    const uint OPEN_EXISTING = 3, CREATE_ALWAYS = 2;

    static byte[] Read(string file)
    {
        using (SafeFileHandle h = CreateFile(@"\\?\" + file + ":Zone.Identifier", GENERIC_READ, 1, IntPtr.Zero,
                                             OPEN_EXISTING, 0, IntPtr.Zero))
        {
            if (h.IsInvalid) return null;
            using (FileStream fs = new FileStream(h, FileAccess.Read))
            {
                if (fs.Length > 64 * 1024) return null;
                byte[] b = new byte[fs.Length];
                int read = 0;
                while (read < b.Length)
                {
                    int r = fs.Read(b, read, b.Length - read);
                    if (r <= 0) break;
                    read += r;
                }
                return b;
            }
        }
    }

    static void Write(string file, byte[] data)
    {
        using (SafeFileHandle h = CreateFile(@"\\?\" + file + ":Zone.Identifier", GENERIC_WRITE, 0, IntPtr.Zero,
                                             CREATE_ALWAYS, 0, IntPtr.Zero))
        {
            if (h.IsInvalid) return;
            using (FileStream fs = new FileStream(h, FileAccess.Write))
                fs.Write(data, 0, data.Length);
        }
    }

    public static void Propagate(string archive, string targetDir)
    {
        try
        {
            if (archive.StartsWith(@"\\")) return;       // сетевые пути — пропускаем
            byte[] mark = Read(archive);
            if (mark == null || mark.Length == 0) return;
            Walk(targetDir, mark);
        }
        catch { }
    }

    static void Walk(string dir, byte[] mark)
    {
        foreach (string f in Directory.GetFiles(dir))
        {
            try
            {
                // Символическая ссылка на файл: запись пометки «прошла бы» по ссылке
                // и изменила файл за пределами папки — такие пропускаем
                if ((File.GetAttributes(f) & FileAttributes.ReparsePoint) != 0) continue;
                Write(f, mark);
            }
            catch { }
        }
        foreach (string d in Directory.GetDirectories(dir))
        {
            // Не заходим в символические ссылки и точки соединения
            try
            {
                if ((File.GetAttributes(d) & FileAttributes.ReparsePoint) != 0) continue;
                Walk(d, mark);
            }
            catch { }
        }
    }
}

// Системный значок архива (тот, которым Windows рисует ZIP-папки).
// Берётся только из системной регистрации (HKLM) — её не может подменить обычный пользователь.
static class SystemIcon
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr LoadLibraryEx(string path, IntPtr file, uint flags);
    [DllImport("kernel32.dll")] static extern bool FreeLibrary(IntPtr h);
    [DllImport("kernel32.dll")] static extern IntPtr FindResource(IntPtr h, IntPtr name, IntPtr type);
    [DllImport("kernel32.dll")] static extern IntPtr LoadResource(IntPtr h, IntPtr res);
    [DllImport("kernel32.dll")] static extern IntPtr LockResource(IntPtr data);
    [DllImport("kernel32.dll")] static extern uint SizeofResource(IntPtr h, IntPtr res);
    delegate bool EnumResNameProc(IntPtr h, IntPtr type, IntPtr name, IntPtr param);
    [DllImport("kernel32.dll")]
    static extern bool EnumResourceNames(IntPtr h, IntPtr type, EnumResNameProc cb, IntPtr param);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr BeginUpdateResource(string file, bool deleteExisting);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool UpdateResource(IntPtr h, IntPtr type, IntPtr name, ushort lang, byte[] data, uint size);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool EndUpdateResource(IntPtr h, bool discard);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool MoveFileEx(string from, string to, uint flags);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern uint ExtractIconEx(string file, int index, IntPtr[] large, IntPtr[] small, uint count);

    static readonly IntPtr RT_ICON = (IntPtr)3;
    static readonly IntPtr RT_GROUP_ICON = (IntPtr)14;

    public static string Reference()
    {
        try
        {
            using (RegistryKey k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Classes\CompressedFolder\DefaultIcon"))
            {
                string s = k == null ? null : k.GetValue("") as string;
                if (!string.IsNullOrEmpty(s)) return s;
            }
        }
        catch { }
        return @"%SystemRoot%\System32\imageres.dll,-174";
    }

    static void Parse(string iconRef, out string path, out int index)
    {
        path = iconRef; index = 0;
        int comma = iconRef.LastIndexOf(',');
        if (comma > 0)
        {
            path = iconRef.Substring(0, comma);
            int.TryParse(iconRef.Substring(comma + 1).Trim(), out index);
        }
        path = Environment.ExpandEnvironmentVariables(path.Trim().Trim('"'));
    }

    // Маленький значок для трея
    public static Icon Small()
    {
        try
        {
            string path; int index;
            Parse(Reference(), out path, out index);
            IntPtr[] small = new IntPtr[1];
            if (ExtractIconEx(path, index, null, small, 1) > 0 && small[0] != IntPtr.Zero)
                return Icon.FromHandle(small[0]);
        }
        catch { }
        return Icon.ExtractAssociatedIcon(Application.ExecutablePath);
    }

    static byte[] Res(IntPtr h, IntPtr name, IntPtr type)
    {
        IntPtr r = FindResource(h, name, type);
        if (r == IntPtr.Zero) return null;
        uint size = SizeofResource(h, r);
        IntPtr p = LockResource(LoadResource(h, r));
        if (p == IntPtr.Zero || size == 0 || size > 16 * 1024 * 1024) return null;
        byte[] b = new byte[size];
        Marshal.Copy(p, b, 0, (int)size);
        return b;
    }

    // Читает группу значков (все размеры) из системной DLL
    static void Read(out byte[] group, out List<byte[]> images)
    {
        string path; int index;
        Parse(Reference(), out path, out index);

        IntPtr h = LoadLibraryEx(path, IntPtr.Zero, 0x2 | 0x20);   // только как ресурсы, без выполнения кода
        if (h == IntPtr.Zero) throw new Win32Exception();
        IntPtr nameMem = IntPtr.Zero;
        try
        {
            IntPtr name;
            if (index < 0) name = (IntPtr)(-index);                 // отрицательный — ID ресурса
            else                                                   // положительный — порядковый номер
            {
                List<object> names = new List<object>();
                EnumResourceNames(h, RT_GROUP_ICON, delegate (IntPtr hh, IntPtr t, IntPtr n, IntPtr pp)
                {
                    if (((long)n >> 16) == 0) names.Add((int)n); else names.Add(Marshal.PtrToStringUni(n));
                    return true;
                }, IntPtr.Zero);
                if (index >= names.Count) throw new Exception("Значок не найден");
                object nm = names[index];
                if (nm is int) name = (IntPtr)(int)nm;
                else { nameMem = Marshal.StringToHGlobalUni((string)nm); name = nameMem; }
            }

            group = Res(h, name, RT_GROUP_ICON);
            if (group == null || group.Length < 6) throw new Exception("Значок не найден");
            int count = BitConverter.ToUInt16(group, 4);
            if (count == 0 || count > 64 || group.Length < 6 + count * 14) throw new Exception("Повреждённый значок");

            images = new List<byte[]>();
            for (int i = 0; i < count; i++)
            {
                int id = BitConverter.ToUInt16(group, 6 + i * 14 + 12);
                byte[] img = Res(h, (IntPtr)id, RT_ICON);
                if (img == null) throw new Exception("Повреждённый значок");
                images.Add(img);
            }
        }
        finally
        {
            if (nameMem != IntPtr.Zero) Marshal.FreeHGlobal(nameMem);
            FreeLibrary(h);
        }
    }

    // Вызывается установщиком (с правами администратора): вшивает системный значок в exe.
    // Запущенный exe нельзя изменить, но можно переименовать — поэтому меняем копию и подменяем.
    public static int StampIntoSelf()
    {
        string exe = Application.ExecutablePath;
        string tmp = exe + ".new";
        try
        {
            byte[] group; List<byte[]> images;
            Read(out group, out images);

            File.Copy(exe, tmp, true);
            IntPtr h = BeginUpdateResource(tmp, false);
            if (h == IntPtr.Zero) throw new Win32Exception();
            bool ok = true;
            for (int i = 0; i < images.Count; i++)
            {
                ok &= UpdateResource(h, RT_ICON, (IntPtr)(i + 1), 0, images[i], (uint)images[i].Length);
                byte[] id = BitConverter.GetBytes((ushort)(i + 1));
                group[6 + i * 14 + 12] = id[0];
                group[6 + i * 14 + 13] = id[1];
            }
            ok &= UpdateResource(h, RT_GROUP_ICON, (IntPtr)1, 0, group, (uint)group.Length);
            if (!EndUpdateResource(h, !ok) || !ok) throw new Exception("Не удалось записать значок");

            string old = exe + "." + Guid.NewGuid().ToString("N") + ".old";
            File.Move(exe, old);
            File.Move(tmp, exe);
            try { File.Delete(old); }
            catch { MoveFileEx(old, null, 4); }            // удалить при перезагрузке
            return 0;
        }
        catch
        {
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
            return 1;                                       // программа работает и без значка
        }
    }
}

// Распаковка средствами Проводника: содержимое архива копируется через IFileOperation —
// тот же движок, что при обычном копировании файлов, поэтому на долгих операциях
// Windows сама показывает своё стандартное окно с прогрессом, скоростью и кнопкой «Отмена».
static class ShellExtract
{
    [ComImport, Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IShellItem { }

    [ComImport, Guid("000214F2-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IEnumIDList
    {
        [PreserveSig] int Next(uint celt, out IntPtr pidl, out uint fetched);
        [PreserveSig] int Skip(uint celt);
        [PreserveSig] int Reset();
        [PreserveSig] int Clone(out IEnumIDList e);
    }

    [ComImport, Guid("000214E6-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IShellFolder
    {
        [PreserveSig] int ParseDisplayName(IntPtr hwnd, IntPtr pbc, [MarshalAs(UnmanagedType.LPWStr)] string name,
                                           IntPtr eaten, out IntPtr pidl, IntPtr attrs);
        [PreserveSig] int EnumObjects(IntPtr hwnd, int flags, out IEnumIDList e);
    }

    [ComImport, Guid("000214EA-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IPersistFolder
    {
        void GetClassID(out Guid clsid);
        [PreserveSig] int Initialize(IntPtr pidl);
    }

    [ComImport, Guid("947aab5f-0a5c-4c13-b4d6-4bf7836fc9f8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IFileOperation
    {
        void Advise(IntPtr sink, out uint cookie);
        void Unadvise(uint cookie);
        void SetOperationFlags(uint flags);
        void SetProgressMessage([MarshalAs(UnmanagedType.LPWStr)] string msg);
        void SetProgressDialog(IntPtr dlg);
        void SetProperties(IntPtr props);
        void SetOwnerWindow(IntPtr hwnd);
        void ApplyPropertiesToItem(IShellItem item);
        void ApplyPropertiesToItems(IntPtr items);
        void RenameItem(IShellItem item, [MarshalAs(UnmanagedType.LPWStr)] string name, IntPtr sink);
        void RenameItems(IntPtr items, [MarshalAs(UnmanagedType.LPWStr)] string name);
        void MoveItem(IShellItem item, IShellItem dest, [MarshalAs(UnmanagedType.LPWStr)] string name, IntPtr sink);
        void MoveItems(IntPtr items, IShellItem dest);
        void CopyItem(IShellItem item, IShellItem dest, [MarshalAs(UnmanagedType.LPWStr)] string name, IntPtr sink);
        void CopyItems(IntPtr items, IShellItem dest);
        void DeleteItem(IShellItem item, IntPtr sink);
        void DeleteItems(IntPtr items);
        void NewItem(IShellItem dest, uint attrs, [MarshalAs(UnmanagedType.LPWStr)] string name,
                     [MarshalAs(UnmanagedType.LPWStr)] string template, IntPtr sink);
        void PerformOperations();
        [return: MarshalAs(UnmanagedType.Bool)] bool GetAnyOperationsAborted();
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern int SHParseDisplayName(string name, IntPtr pbc, out IntPtr pidl, uint sfgaoIn, out uint sfgaoOut);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern int SHCreateItemFromParsingName(string path, IntPtr pbc, ref Guid riid,
        [MarshalAs(UnmanagedType.Interface)] out IShellItem item);

    [DllImport("shell32.dll")]
    static extern int SHCreateItemWithParent(IntPtr pidlParent, IShellFolder psfParent, IntPtr pidl, ref Guid riid,
        [MarshalAs(UnmanagedType.Interface)] out IShellItem item);

    static readonly Guid CLSID_FileOperation = new Guid("3ad05575-8857-4850-9277-11b85bdb8e09");
    static Guid IID_IShellItem = new Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe");

    const uint FOF_NOCONFIRMATION = 0x0010;   // без вопросов «заменить?»
    const uint FOF_NOCONFIRMMKDIR = 0x0200;   // без вопросов о создании папок
    const uint FOF_NOERRORUI      = 0x0400;   // без диалогов об ошибках — ошибку покажем сами
    const uint FOFX_NOMINIMIZEBOX = 0x01000000;

    const int SHCONTF_FOLDERS = 0x20, SHCONTF_NONFOLDERS = 0x40,
              SHCONTF_INCLUDEHIDDEN = 0x80, SHCONTF_INCLUDESUPERHIDDEN = 0x10000;

    static Guid? HandlerFor(string archive)
    {
        string progId;
        return Lookup(archive, out progId);
    }

    // Системный тип файла (например, CompressedFolder), если у Windows есть встроенный просмотр
    public static string SystemProgId(string archive)
    {
        string progId;
        return Lookup(archive, out progId) == null ? null : progId;
    }

    // Обработчик архивов, встроенный в Windows для этого расширения. Берём из HKLM — это
    // системная регистрация, на неё не влияет, какая программа назначена «по умолчанию».
    static Guid? Lookup(string archive, out string progId)
    {
        progId = null;
        string ext = Path.GetExtension(archive);
        if (string.IsNullOrEmpty(ext)) return null;
        try
        {
            using (RegistryKey k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Classes\" + ext))
                if (k != null) progId = k.GetValue("") as string;
            if (string.IsNullOrEmpty(progId) || progId.StartsWith("AutoUnpack", StringComparison.OrdinalIgnoreCase))
                return null;
            using (RegistryKey k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Classes\" + progId + @"\CLSID"))
            {
                string s = k == null ? null : k.GetValue("") as string;
                if (string.IsNullOrEmpty(s)) return null;
                return new Guid(s);
            }
        }
        catch { return null; }
    }

    // true — распаковано; false — Проводник этот формат не открывает (нужен запасной способ).
    // OperationCanceledException — пользователь нажал «Отмена».
    public static bool Run(string archive, string targetDir)
    {
        Guid? clsid = HandlerFor(archive);
        if (clsid == null) return false;

        IntPtr pidlArchive = IntPtr.Zero;
        List<IntPtr> children = new List<IntPtr>();
        try
        {
            uint sfgao;
            if (SHParseDisplayName(archive, IntPtr.Zero, out pidlArchive, 0, out sfgao) != 0) return false;

            Type t = Type.GetTypeFromCLSID(clsid.Value);
            object handler = Activator.CreateInstance(t);
            IPersistFolder pf = handler as IPersistFolder;
            IShellFolder folder = handler as IShellFolder;
            if (pf == null || folder == null || pf.Initialize(pidlArchive) != 0) return false;

            IEnumIDList en;
            int flags = SHCONTF_FOLDERS | SHCONTF_NONFOLDERS | SHCONTF_INCLUDEHIDDEN | SHCONTF_INCLUDESUPERHIDDEN;
            if (folder.EnumObjects(IntPtr.Zero, flags, out en) != 0 || en == null) return false;

            IntPtr child; uint fetched;
            while (en.Next(1, out child, out fetched) == 0 && fetched == 1)
                children.Add(child);

            if (children.Count == 0) return true;   // пустой архив

            IShellItem dest;
            if (SHCreateItemFromParsingName(targetDir, IntPtr.Zero, ref IID_IShellItem, out dest) != 0) return false;

            IFileOperation op = (IFileOperation)Activator.CreateInstance(Type.GetTypeFromCLSID(CLSID_FileOperation));
            op.SetOperationFlags(FOF_NOCONFIRMATION | FOF_NOCONFIRMMKDIR | FOF_NOERRORUI | FOFX_NOMINIMIZEBOX);

            foreach (IntPtr c in children)
            {
                IShellItem item;
                if (SHCreateItemWithParent(pidlArchive, folder, c, ref IID_IShellItem, out item) != 0)
                    return false;
                op.CopyItem(item, dest, null, IntPtr.Zero);
            }

            try
            {
                op.PerformOperations();
            }
            catch (COMException e)
            {
                if ((uint)e.ErrorCode == 0x80270000 || (uint)e.ErrorCode == 0x800704C7)   // отменено
                    throw new OperationCanceledException();
                throw new Exception(e.Message);
            }
            if (op.GetAnyOperationsAborted()) throw new OperationCanceledException();
            return true;
        }
        finally
        {
            foreach (IntPtr c in children) Marshal.FreeCoTaskMem(c);
            if (pidlArchive != IntPtr.Zero) Marshal.FreeCoTaskMem(pidlArchive);
        }
    }
}
