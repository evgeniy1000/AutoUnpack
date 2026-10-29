<#
  Сборка AutoUnpack: программа (dist\AutoUnpack.exe) и установщик (dist\AutoUnpack-Setup-<версия>.exe).
  Нужны только компоненты Windows (.NET Framework 4.x, csc.exe) и Inno Setup 6.3+.

  Примеры:
    .\build.ps1                      # версия 1.0.0
    .\build.ps1 -Version 1.2.0
    .\build.ps1 -SkipInstaller       # только exe
#>
param(
    [string]$Version = '1.0.0',
    [string]$Url = 'https://github.com/',
    [switch]$SkipInstaller
)
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot

if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw "Версия должна быть вида 1.2.3, получено: $Version" }

$dist = Join-Path $PSScriptRoot 'dist'
$obj  = Join-Path $PSScriptRoot 'obj'
New-Item -ItemType Directory -Force -Path $dist, $obj | Out-Null

# ---------- версия сборки ----------
$versionCs = Join-Path $obj 'Version.cs'
@"
using System.Reflection;
[assembly: AssemblyTitle("AutoUnpack")]
[assembly: AssemblyDescription("AutoUnpack")]
[assembly: AssemblyProduct("AutoUnpack")]
[assembly: AssemblyCopyright("MIT License")]
[assembly: AssemblyVersion("$Version.0")]
[assembly: AssemblyFileVersion("$Version.0")]
[assembly: AssemblyInformationalVersion("$Version")]
"@ | Set-Content -LiteralPath $versionCs -Encoding UTF8

# ---------- компиляция ----------
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $csc)) { $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if (-not (Test-Path -LiteralPath $csc)) { throw 'Не найден csc.exe (.NET Framework 4).' }

$exe = Join-Path $dist 'AutoUnpack.exe'
Write-Host "Сборка $exe (версия $Version)..."
$out = & $csc /nologo /target:winexe /optimize+ /codepage:65001 /platform:anycpu "/out:$exe" `
    /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Core.dll `
    /reference:Microsoft.CSharp.dll `
    'src\AutoUnpack.cs' "$versionCs" 2>&1 | Out-String
Write-Host $out
if ($LASTEXITCODE -ne 0) { throw 'Компиляция не удалась.' }

if ($SkipInstaller) { return }

# ---------- установщик ----------
$iscc = $null
foreach ($p in @("${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe", "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
                 "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe")) {
    if (Test-Path -LiteralPath $p) { $iscc = $p; break }
}
if (-not $iscc) {
    $cmd = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($cmd) { $iscc = $cmd.Source }
}
if (-not $iscc) { throw 'Не найден Inno Setup 6 (ISCC.exe). Установите: winget install JRSoftware.InnoSetup' }

Write-Host 'Сборка установщика...'
& $iscc /Q "/DMyVersion=$Version" "/DMyUrl=$Url" 'installer\AutoUnpack.iss'
if ($LASTEXITCODE -ne 0) { throw 'Сборка установщика не удалась.' }

Get-ChildItem -LiteralPath $dist | Format-Table Name, Length
