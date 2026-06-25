$ErrorActionPreference = 'Stop'
$appName = 'Rechnung'
$targetRoot = Join-Path $env:LOCALAPPDATA $appName
$shortcutPath = Join-Path ([Environment]::GetFolderPath('Desktop')) 'Rechnung.lnk'

if (Test-Path $targetRoot) {
	Remove-Item $targetRoot -Recurse -Force
}

if (Test-Path $shortcutPath) {
	Remove-Item $shortcutPath -Force
}

Write-Host 'Kaldırma tamamlandı.'
