$ErrorActionPreference = 'Stop'
$appName = 'Rechnung'
$targetRoot = Join-Path $env:LOCALAPPDATA $appName
$targetDir = Join-Path $targetRoot 'app'
$shortcutPath = Join-Path ([Environment]::GetFolderPath('Desktop')) 'Rechnung.lnk'

if (Test-Path $targetDir) {
	Remove-Item $targetDir -Recurse -Force
}

if (Test-Path $targetRoot) {
	$remaining = Get-ChildItem $targetRoot -Force -ErrorAction SilentlyContinue
	if (-not $remaining) {
		Remove-Item $targetRoot -Force
	}
}

if (Test-Path $shortcutPath) {
	Remove-Item $shortcutPath -Force
}

Write-Host 'Kaldırma tamamlandı.'
Write-Host 'Kullanıcı verileri ve arşiv içerikleri korunmuştur.'
