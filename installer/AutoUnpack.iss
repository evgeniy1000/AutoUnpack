; Установщик «Распаковщик архивов» (AutoUnpack) — Inno Setup 6.3+
; Сборка: ISCC.exe /DMyVersion=1.0.0 installer\AutoUnpack.iss  (или build.ps1)

#ifndef MyVersion
  #define MyVersion "1.0.0"
#endif
#ifndef MyUrl
  #define MyUrl "https://github.com/"
#endif

[Setup]
AppId={{66C07F43-3A45-45E8-AD49-56850063631C}
AppName=Распаковщик архивов (AutoUnpack)
AppVersion={#MyVersion}
AppVerName=Распаковщик архивов {#MyVersion}
AppPublisher=AutoUnpack
AppPublisherURL={#MyUrl}
AppSupportURL={#MyUrl}
AppUpdatesURL={#MyUrl}
DefaultDirName={autopf}\AutoUnpack
DisableProgramGroupPage=yes
; Папка установки — только Program Files: каталог, доступный пользователю на запись,
; позволил бы подложить DLL, которую загрузит exe при запуске с правами администратора
DisableDirPage=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
OutputDir=..\dist
OutputBaseFilename=AutoUnpack-Setup-{#MyVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ChangesAssociations=yes
CloseApplications=no
LicenseFile=..\LICENSE
UninstallDisplayIcon={app}\AutoUnpack.exe
UninstallDisplayName=Распаковщик архивов (AutoUnpack)
VersionInfoVersion={#MyVersion}
VersionInfoDescription=Установщик «Распаковщик архивов»
SetupLogging=yes

[Languages]
Name: "ru"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "en"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
ru.ExtractHere=Распаковать в папку рядом
en.ExtractHere=Extract to folder here
ru.ViewArchive=Просмотреть архив
en.ViewArchive=View archive
ru.AutostartTask=Запускать вместе с Windows (значок в трее)
en.AutostartTask=Start with Windows (tray icon)
ru.LaunchTray=Запустить распаковщик
en.LaunchTray=Launch AutoUnpack
ru.DefaultApps=Назначить распаковщиком архивов по умолчанию (откроются Параметры Windows)
en.DefaultApps=Make it the default for archives (opens Windows Settings)
ru.StampingIcon=Настройка значка...
en.StampingIcon=Setting up the icon...
ru.TypeName=Архив
en.TypeName=Archive
ru.AppTitle=Распаковщик архивов
en.AppTitle=AutoUnpack
ru.AppDescription=Распаковка архивов двойным кликом в папку рядом с архивом
en.AppDescription=Double-click an archive to extract it into a folder next to it
ru.FriendlyName=Распаковщик (в папку рядом)
en.FriendlyName=AutoUnpack (extract here)

[Tasks]
Name: "autostart"; Description: "{cm:AutostartTask}"

[Files]
Source: "..\dist\AutoUnpack.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "ReadMe.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; DestName: "LICENSE.txt"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\AutoUnpack"; Filename: "{app}\AutoUnpack.exe"; Parameters: "/tray"; Comment: "{cm:AppDescription}"

[Registry]
; Типы файлов: отдельный для каждого формата, как у самой Windows.
; Значок — ровно системный значок ZIP-папки этой Windows.
; CLSID — системный обработчик формата: благодаря ему архив остаётся «папкой» для Проводника
; (просмотр по Shift, навигация внутрь архива), даже когда программой по умолчанию выбран AutoUnpack.
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.zip"; ValueType: string; ValueName: ""; ValueData: "{cm:TypeName}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.zip\DefaultIcon"; ValueType: expandsz; ValueName: ""; ValueData: "{code:ArchiveIcon}"
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.zip\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AutoUnpack.exe"" ""%1"""
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.zip\CLSID"; ValueType: string; ValueName: ""; ValueData: "{code:HandlerClsid|.zip}"; Check: HasHandler('.zip')
Root: HKLM; Subkey: "Software\Classes\.zip\OpenWithProgids"; ValueType: string; ValueName: "AutoUnpack.zip"; ValueData: ""; Flags: uninsdeletevalue
Root: HKLM; Subkey: "Software\AutoUnpack\Capabilities\FileAssociations"; ValueType: string; ValueName: ".zip"; ValueData: "AutoUnpack.zip"
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.7z"; ValueType: string; ValueName: ""; ValueData: "{cm:TypeName}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.7z\DefaultIcon"; ValueType: expandsz; ValueName: ""; ValueData: "{code:ArchiveIcon}"
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.7z\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AutoUnpack.exe"" ""%1"""
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.7z\CLSID"; ValueType: string; ValueName: ""; ValueData: "{code:HandlerClsid|.7z}"; Check: HasHandler('.7z')
Root: HKLM; Subkey: "Software\Classes\.7z\OpenWithProgids"; ValueType: string; ValueName: "AutoUnpack.7z"; ValueData: ""; Flags: uninsdeletevalue
Root: HKLM; Subkey: "Software\AutoUnpack\Capabilities\FileAssociations"; ValueType: string; ValueName: ".7z"; ValueData: "AutoUnpack.7z"
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.rar"; ValueType: string; ValueName: ""; ValueData: "{cm:TypeName}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.rar\DefaultIcon"; ValueType: expandsz; ValueName: ""; ValueData: "{code:ArchiveIcon}"
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.rar\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AutoUnpack.exe"" ""%1"""
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.rar\CLSID"; ValueType: string; ValueName: ""; ValueData: "{code:HandlerClsid|.rar}"; Check: HasHandler('.rar')
Root: HKLM; Subkey: "Software\Classes\.rar\OpenWithProgids"; ValueType: string; ValueName: "AutoUnpack.rar"; ValueData: ""; Flags: uninsdeletevalue
Root: HKLM; Subkey: "Software\AutoUnpack\Capabilities\FileAssociations"; ValueType: string; ValueName: ".rar"; ValueData: "AutoUnpack.rar"
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.tar"; ValueType: string; ValueName: ""; ValueData: "{cm:TypeName}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.tar\DefaultIcon"; ValueType: expandsz; ValueName: ""; ValueData: "{code:ArchiveIcon}"
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.tar\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AutoUnpack.exe"" ""%1"""
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.tar\CLSID"; ValueType: string; ValueName: ""; ValueData: "{code:HandlerClsid|.tar}"; Check: HasHandler('.tar')
Root: HKLM; Subkey: "Software\Classes\.tar\OpenWithProgids"; ValueType: string; ValueName: "AutoUnpack.tar"; ValueData: ""; Flags: uninsdeletevalue
Root: HKLM; Subkey: "Software\AutoUnpack\Capabilities\FileAssociations"; ValueType: string; ValueName: ".tar"; ValueData: "AutoUnpack.tar"
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.tgz"; ValueType: string; ValueName: ""; ValueData: "{cm:TypeName}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.tgz\DefaultIcon"; ValueType: expandsz; ValueName: ""; ValueData: "{code:ArchiveIcon}"
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.tgz\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AutoUnpack.exe"" ""%1"""
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.tgz\CLSID"; ValueType: string; ValueName: ""; ValueData: "{code:HandlerClsid|.tgz}"; Check: HasHandler('.tgz')
Root: HKLM; Subkey: "Software\Classes\.tgz\OpenWithProgids"; ValueType: string; ValueName: "AutoUnpack.tgz"; ValueData: ""; Flags: uninsdeletevalue
Root: HKLM; Subkey: "Software\AutoUnpack\Capabilities\FileAssociations"; ValueType: string; ValueName: ".tgz"; ValueData: "AutoUnpack.tgz"
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.gz"; ValueType: string; ValueName: ""; ValueData: "{cm:TypeName}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.gz\DefaultIcon"; ValueType: expandsz; ValueName: ""; ValueData: "{code:ArchiveIcon}"
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.gz\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AutoUnpack.exe"" ""%1"""
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.gz\CLSID"; ValueType: string; ValueName: ""; ValueData: "{code:HandlerClsid|.gz}"; Check: HasHandler('.gz')
Root: HKLM; Subkey: "Software\Classes\.gz\OpenWithProgids"; ValueType: string; ValueName: "AutoUnpack.gz"; ValueData: ""; Flags: uninsdeletevalue
Root: HKLM; Subkey: "Software\AutoUnpack\Capabilities\FileAssociations"; ValueType: string; ValueName: ".gz"; ValueData: "AutoUnpack.gz"
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.bz2"; ValueType: string; ValueName: ""; ValueData: "{cm:TypeName}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.bz2\DefaultIcon"; ValueType: expandsz; ValueName: ""; ValueData: "{code:ArchiveIcon}"
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.bz2\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AutoUnpack.exe"" ""%1"""
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.bz2\CLSID"; ValueType: string; ValueName: ""; ValueData: "{code:HandlerClsid|.bz2}"; Check: HasHandler('.bz2')
Root: HKLM; Subkey: "Software\Classes\.bz2\OpenWithProgids"; ValueType: string; ValueName: "AutoUnpack.bz2"; ValueData: ""; Flags: uninsdeletevalue
Root: HKLM; Subkey: "Software\AutoUnpack\Capabilities\FileAssociations"; ValueType: string; ValueName: ".bz2"; ValueData: "AutoUnpack.bz2"
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.tbz2"; ValueType: string; ValueName: ""; ValueData: "{cm:TypeName}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.tbz2\DefaultIcon"; ValueType: expandsz; ValueName: ""; ValueData: "{code:ArchiveIcon}"
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.tbz2\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AutoUnpack.exe"" ""%1"""
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.tbz2\CLSID"; ValueType: string; ValueName: ""; ValueData: "{code:HandlerClsid|.tbz2}"; Check: HasHandler('.tbz2')
Root: HKLM; Subkey: "Software\Classes\.tbz2\OpenWithProgids"; ValueType: string; ValueName: "AutoUnpack.tbz2"; ValueData: ""; Flags: uninsdeletevalue
Root: HKLM; Subkey: "Software\AutoUnpack\Capabilities\FileAssociations"; ValueType: string; ValueName: ".tbz2"; ValueData: "AutoUnpack.tbz2"
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.xz"; ValueType: string; ValueName: ""; ValueData: "{cm:TypeName}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.xz\DefaultIcon"; ValueType: expandsz; ValueName: ""; ValueData: "{code:ArchiveIcon}"
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.xz\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AutoUnpack.exe"" ""%1"""
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.xz\CLSID"; ValueType: string; ValueName: ""; ValueData: "{code:HandlerClsid|.xz}"; Check: HasHandler('.xz')
Root: HKLM; Subkey: "Software\Classes\.xz\OpenWithProgids"; ValueType: string; ValueName: "AutoUnpack.xz"; ValueData: ""; Flags: uninsdeletevalue
Root: HKLM; Subkey: "Software\AutoUnpack\Capabilities\FileAssociations"; ValueType: string; ValueName: ".xz"; ValueData: "AutoUnpack.xz"
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.txz"; ValueType: string; ValueName: ""; ValueData: "{cm:TypeName}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.txz\DefaultIcon"; ValueType: expandsz; ValueName: ""; ValueData: "{code:ArchiveIcon}"
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.txz\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AutoUnpack.exe"" ""%1"""
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.txz\CLSID"; ValueType: string; ValueName: ""; ValueData: "{code:HandlerClsid|.txz}"; Check: HasHandler('.txz')
Root: HKLM; Subkey: "Software\Classes\.txz\OpenWithProgids"; ValueType: string; ValueName: "AutoUnpack.txz"; ValueData: ""; Flags: uninsdeletevalue
Root: HKLM; Subkey: "Software\AutoUnpack\Capabilities\FileAssociations"; ValueType: string; ValueName: ".txz"; ValueData: "AutoUnpack.txz"
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.zst"; ValueType: string; ValueName: ""; ValueData: "{cm:TypeName}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.zst\DefaultIcon"; ValueType: expandsz; ValueName: ""; ValueData: "{code:ArchiveIcon}"
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.zst\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AutoUnpack.exe"" ""%1"""
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.zst\CLSID"; ValueType: string; ValueName: ""; ValueData: "{code:HandlerClsid|.zst}"; Check: HasHandler('.zst')
Root: HKLM; Subkey: "Software\Classes\.zst\OpenWithProgids"; ValueType: string; ValueName: "AutoUnpack.zst"; ValueData: ""; Flags: uninsdeletevalue
Root: HKLM; Subkey: "Software\AutoUnpack\Capabilities\FileAssociations"; ValueType: string; ValueName: ".zst"; ValueData: "AutoUnpack.zst"
; Прежний общий тип (версии 1.0.0–1.0.2): сохраняется, чтобы уже сделанный выбор
; «по умолчанию» продолжал работать. В основном его выбирали для .zip.
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.Archive"; ValueType: string; ValueName: ""; ValueData: "{cm:TypeName}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.Archive\DefaultIcon"; ValueType: expandsz; ValueName: ""; ValueData: "{code:ArchiveIcon}"
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.Archive\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AutoUnpack.exe"" ""%1"""
Root: HKLM; Subkey: "Software\Classes\AutoUnpack.Archive\CLSID"; ValueType: string; ValueName: ""; ValueData: "{code:HandlerClsid|.zip}"; Check: HasHandler('.zip')

; Приложение — для списка «Открыть с помощью»
Root: HKLM; Subkey: "Software\Classes\Applications\AutoUnpack.exe"; ValueType: string; ValueName: "FriendlyAppName"; ValueData: "{cm:FriendlyName}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\Applications\AutoUnpack.exe\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AutoUnpack.exe"" ""%1"""

; Регистрация в «Приложениях по умолчанию» Windows
Root: HKLM; Subkey: "Software\AutoUnpack"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\AutoUnpack\Capabilities"; ValueType: string; ValueName: "ApplicationName"; ValueData: "{cm:AppTitle}"
Root: HKLM; Subkey: "Software\AutoUnpack\Capabilities"; ValueType: string; ValueName: "ApplicationDescription"; ValueData: "{cm:AppDescription}"
Root: HKLM; Subkey: "Software\AutoUnpack\Capabilities"; ValueType: string; ValueName: "ApplicationIcon"; ValueData: """{app}\AutoUnpack.exe"",0"
Root: HKLM; Subkey: "Software\RegisteredApplications"; ValueType: string; ValueName: "AutoUnpack"; ValueData: "Software\AutoUnpack\Capabilities"; Flags: uninsdeletevalue

; Автозапуск значка в трее (галочку потом можно менять в меню значка)
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "AutoUnpack"; ValueData: """{app}\AutoUnpack.exe"" /tray"; Flags: uninsdeletevalue; Tasks: autostart
; Настройки пользователя — удаляются вместе с программой
Root: HKCU; Subkey: "Software\AutoUnpack"; Flags: uninsdeletekey dontcreatekey

; Расширения: «Открыть с помощью», «Приложения по умолчанию», пункт контекстного меню
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.zip\shell\AutoUnpack"; ValueType: string; ValueName: ""; ValueData: "{cm:ExtractHere}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.zip\shell\AutoUnpack"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\AutoUnpack.exe"",0"
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.zip\shell\AutoUnpack\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AutoUnpack.exe"" /extract ""%1"""
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.zip\shell\AutoUnpackView"; ValueType: string; ValueName: ""; ValueData: "{cm:ViewArchive}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.zip\shell\AutoUnpackView"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\AutoUnpack.exe"",0"
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.zip\shell\AutoUnpackView\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AutoUnpack.exe"" /view ""%1"""
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.7z\shell\AutoUnpack"; ValueType: string; ValueName: ""; ValueData: "{cm:ExtractHere}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.7z\shell\AutoUnpack"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\AutoUnpack.exe"",0"
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.7z\shell\AutoUnpack\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AutoUnpack.exe"" /extract ""%1"""
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.7z\shell\AutoUnpackView"; ValueType: string; ValueName: ""; ValueData: "{cm:ViewArchive}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.7z\shell\AutoUnpackView"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\AutoUnpack.exe"",0"
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.7z\shell\AutoUnpackView\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AutoUnpack.exe"" /view ""%1"""
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.rar\shell\AutoUnpack"; ValueType: string; ValueName: ""; ValueData: "{cm:ExtractHere}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.rar\shell\AutoUnpack"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\AutoUnpack.exe"",0"
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.rar\shell\AutoUnpack\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AutoUnpack.exe"" /extract ""%1"""
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.rar\shell\AutoUnpackView"; ValueType: string; ValueName: ""; ValueData: "{cm:ViewArchive}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.rar\shell\AutoUnpackView"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\AutoUnpack.exe"",0"
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.rar\shell\AutoUnpackView\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AutoUnpack.exe"" /view ""%1"""
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.tar\shell\AutoUnpack"; ValueType: string; ValueName: ""; ValueData: "{cm:ExtractHere}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.tar\shell\AutoUnpack"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\AutoUnpack.exe"",0"
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.tar\shell\AutoUnpack\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AutoUnpack.exe"" /extract ""%1"""
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.tar\shell\AutoUnpackView"; ValueType: string; ValueName: ""; ValueData: "{cm:ViewArchive}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.tar\shell\AutoUnpackView"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\AutoUnpack.exe"",0"
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.tar\shell\AutoUnpackView\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AutoUnpack.exe"" /view ""%1"""
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.tgz\shell\AutoUnpack"; ValueType: string; ValueName: ""; ValueData: "{cm:ExtractHere}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.tgz\shell\AutoUnpack"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\AutoUnpack.exe"",0"
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.tgz\shell\AutoUnpack\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AutoUnpack.exe"" /extract ""%1"""
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.tgz\shell\AutoUnpackView"; ValueType: string; ValueName: ""; ValueData: "{cm:ViewArchive}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.tgz\shell\AutoUnpackView"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\AutoUnpack.exe"",0"
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.tgz\shell\AutoUnpackView\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AutoUnpack.exe"" /view ""%1"""
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.gz\shell\AutoUnpack"; ValueType: string; ValueName: ""; ValueData: "{cm:ExtractHere}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.gz\shell\AutoUnpack"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\AutoUnpack.exe"",0"
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.gz\shell\AutoUnpack\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AutoUnpack.exe"" /extract ""%1"""
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.gz\shell\AutoUnpackView"; ValueType: string; ValueName: ""; ValueData: "{cm:ViewArchive}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.gz\shell\AutoUnpackView"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\AutoUnpack.exe"",0"
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.gz\shell\AutoUnpackView\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AutoUnpack.exe"" /view ""%1"""
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.bz2\shell\AutoUnpack"; ValueType: string; ValueName: ""; ValueData: "{cm:ExtractHere}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.bz2\shell\AutoUnpack"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\AutoUnpack.exe"",0"
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.bz2\shell\AutoUnpack\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AutoUnpack.exe"" /extract ""%1"""
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.bz2\shell\AutoUnpackView"; ValueType: string; ValueName: ""; ValueData: "{cm:ViewArchive}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.bz2\shell\AutoUnpackView"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\AutoUnpack.exe"",0"
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.bz2\shell\AutoUnpackView\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AutoUnpack.exe"" /view ""%1"""
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.tbz2\shell\AutoUnpack"; ValueType: string; ValueName: ""; ValueData: "{cm:ExtractHere}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.tbz2\shell\AutoUnpack"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\AutoUnpack.exe"",0"
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.tbz2\shell\AutoUnpack\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AutoUnpack.exe"" /extract ""%1"""
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.tbz2\shell\AutoUnpackView"; ValueType: string; ValueName: ""; ValueData: "{cm:ViewArchive}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.tbz2\shell\AutoUnpackView"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\AutoUnpack.exe"",0"
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.tbz2\shell\AutoUnpackView\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AutoUnpack.exe"" /view ""%1"""
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.xz\shell\AutoUnpack"; ValueType: string; ValueName: ""; ValueData: "{cm:ExtractHere}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.xz\shell\AutoUnpack"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\AutoUnpack.exe"",0"
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.xz\shell\AutoUnpack\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AutoUnpack.exe"" /extract ""%1"""
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.xz\shell\AutoUnpackView"; ValueType: string; ValueName: ""; ValueData: "{cm:ViewArchive}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.xz\shell\AutoUnpackView"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\AutoUnpack.exe"",0"
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.xz\shell\AutoUnpackView\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AutoUnpack.exe"" /view ""%1"""
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.txz\shell\AutoUnpack"; ValueType: string; ValueName: ""; ValueData: "{cm:ExtractHere}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.txz\shell\AutoUnpack"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\AutoUnpack.exe"",0"
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.txz\shell\AutoUnpack\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AutoUnpack.exe"" /extract ""%1"""
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.txz\shell\AutoUnpackView"; ValueType: string; ValueName: ""; ValueData: "{cm:ViewArchive}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.txz\shell\AutoUnpackView"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\AutoUnpack.exe"",0"
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.txz\shell\AutoUnpackView\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AutoUnpack.exe"" /view ""%1"""
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.zst\shell\AutoUnpack"; ValueType: string; ValueName: ""; ValueData: "{cm:ExtractHere}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.zst\shell\AutoUnpack"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\AutoUnpack.exe"",0"
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.zst\shell\AutoUnpack\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AutoUnpack.exe"" /extract ""%1"""
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.zst\shell\AutoUnpackView"; ValueType: string; ValueName: ""; ValueData: "{cm:ViewArchive}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.zst\shell\AutoUnpackView"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\AutoUnpack.exe"",0"
Root: HKLM; Subkey: "Software\Classes\SystemFileAssociations\.zst\shell\AutoUnpackView\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AutoUnpack.exe"" /view ""%1"""

[Run]
; Вшить в AutoUnpack.exe системный значок архива (берётся из этой Windows, поэтому совпадает 1:1)
Filename: "{app}\AutoUnpack.exe"; Parameters: "/stampicon"; StatusMsg: "{cm:StampingIcon}"; Flags: runhidden waituntilterminated
Filename: "{sys}\ie4uinit.exe"; Parameters: "-show"; Flags: runhidden waituntilterminated skipifdoesntexist
Filename: "{app}\AutoUnpack.exe"; Parameters: "/tray"; Description: "{cm:LaunchTray}"; Flags: postinstall nowait runasoriginaluser skipifsilent
Filename: "ms-settings:defaultapps?registeredAppMachine=AutoUnpack"; Description: "{cm:DefaultApps}"; Flags: postinstall shellexec nowait runasoriginaluser skipifsilent

[UninstallRun]
Filename: "{sys}\taskkill.exe"; Parameters: "/f /im AutoUnpack.exe"; Flags: runhidden waituntilterminated; RunOnceId: "KillTray"

[UninstallDelete]
Type: files; Name: "{app}\AutoUnpack.exe.*"

[Code]
const
  Exts = '.zip,.7z,.rar,.tar,.tgz,.gz,.bz2,.tbz2,.xz,.txz,.zst';

// Системный значок ZIP-папки этой Windows (только из HKLM)
function ArchiveIcon(Param: String): String;
begin
  if not RegQueryStringValue(HKLM, 'SOFTWARE\Classes\CompressedFolder\DefaultIcon', '', Result) or (Result = '') then
    Result := '%SystemRoot%\System32\imageres.dll,-174';
end;

// Системный обработчик формата (например, CompressedFolder для .zip) — только из HKLM
function HandlerClsid(Ext: String): String;
var
  progId: String;
begin
  Result := '';
  if not RegQueryStringValue(HKLM, 'SOFTWARE\Classes\' + Ext, '', progId) then exit;
  if (progId = '') or (Pos('autounpack', Lowercase(progId)) = 1) then exit;
  if not RegQueryStringValue(HKLM, 'SOFTWARE\Classes\' + progId + '\CLSID', '', Result) then
    Result := '';
end;

function HasHandler(Ext: String): Boolean;
begin
  Result := HandlerClsid(Ext) <> '';
end;

procedure KillTray;
var
  rc: Integer;
begin
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/f /im AutoUnpack.exe', '', SW_HIDE, ewWaitUntilTerminated, rc);
  Sleep(500);
end;

// Убрать следы первой версии, которая ставилась в профиль пользователя (%LOCALAPPDATA%)
procedure RemoveOldPerUserVersion;
var
  list: TStringList;
  i: Integer;
  ext: String;
begin
  RegDeleteKeyIncludingSubkeys(HKCU, 'Software\Classes\AutoUnpack.Archive');
  RegDeleteKeyIncludingSubkeys(HKCU, 'Software\Classes\Applications\AutoUnpack.exe');
  list := TStringList.Create;
  try
    list.CommaText := Exts;
    for i := 0 to list.Count - 1 do
    begin
      ext := list[i];
      RegDeleteValue(HKCU, 'Software\Classes\' + ext + '\OpenWithProgids', 'AutoUnpack.Archive');
      RegDeleteKeyIncludingSubkeys(HKCU, 'Software\Classes\SystemFileAssociations\' + ext + '\shell\AutoUnpack');
    end;
  finally
    list.Free;
  end;
  // Старый автозапуск указывал на %LOCALAPPDATA% — удаляем только его
  if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'AutoUnpack', ext) and
     (Pos(Lowercase(ExpandConstant('{localappdata}')), Lowercase(ext)) > 0) then
    RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'AutoUnpack');
  DelTree(ExpandConstant('{localappdata}\AutoUnpack'), True, True, True);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  KillTray;
  RemoveOldPerUserVersion;
  Result := '';
end;
