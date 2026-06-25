$ErrorActionPreference = 'Stop'
$appName = 'Rechnung'
$sourceDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$targetRoot = Join-Path $env:LOCALAPPDATA $appName
$targetDir = Join-Path $targetRoot 'app'
$shortcutPath = Join-Path ([Environment]::GetFolderPath('Desktop')) 'Rechnung.lnk'

if (Test-Path $targetDir) {
	Remove-Item $targetDir -Recurse -Force
}

New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
Copy-Item (Join-Path $sourceDir '*') $targetDir -Recurse -Force

$wshell = New-Object -ComObject WScript.Shell
$shortcut = $wshell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = Join-Path $targetDir 'Rechnung.exe'
$shortcut.WorkingDirectory = $targetDir
$shortcut.IconLocation = Join-Path $targetDir 'Rechnung.exe'
$shortcut.Save()

Write-Host 'Kurulum tamamlandı.'
Write-Host "Uygulama klasörü: $targetDir"
Write-Host 'Kullanıcı verileri ayrı tutulur ve AppData klasörlerinde korunur.'
