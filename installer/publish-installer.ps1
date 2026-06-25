param(
	[string]$Configuration = "Release",
	[string]$Runtime = "win-x64",
	[string]$ProjectPath = "..\Örnek\Örnek.csproj",
	[string]$PublishDir = ".\artifacts\publish",
	[string]$Version,
	[string]$UpdateBaseUrl = "",
	[switch]$AutoIncrementVersion,
	[switch]$BuildInstaller
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptRoot

$versionFilePath = Join-Path $scriptRoot "version.txt"

function Get-NextVersion {
	param(
		[string]$CurrentVersion
	)

	$parts = $CurrentVersion.Split('.')
	if ($parts.Count -ne 3) {
		throw "version.txt içeriği Major.Minor.Patch formatında olmalıdır."
	}

	[int]$major = $parts[0]
	[int]$minor = $parts[1]
	[int]$patch = $parts[2]
	$patch++

	return "$major.$minor.$patch"
}

if (-not (Test-Path $versionFilePath)) {
	Set-Content -Path $versionFilePath -Value "1.0.0" -Encoding UTF8
}

if (-not $Version) {
	$Version = (Get-Content -Path $versionFilePath -Raw).Trim()
	if (-not $Version) {
		$Version = "1.0.0"
	}

	if ($AutoIncrementVersion) {
		$Version = Get-NextVersion -CurrentVersion $Version
		Set-Content -Path $versionFilePath -Value $Version -Encoding UTF8
	}
}

$projectFullPath = Resolve-Path $ProjectPath
$publishFullPath = Join-Path $scriptRoot $PublishDir
$installerOutputDir = Join-Path $scriptRoot "artifacts\setup"
$updateManifestPath = Join-Path $installerOutputDir "latest.json"
$bundledUpdateSettingsPath = Join-Path $publishFullPath "update-settings.json"

if (Test-Path $publishFullPath) {
	Remove-Item $publishFullPath -Recurse -Force
}

New-Item -ItemType Directory -Path $publishFullPath -Force | Out-Null
New-Item -ItemType Directory -Path $installerOutputDir -Force | Out-Null

Write-Host "Publish başlatılıyor..." -ForegroundColor Cyan
Write-Host "Sürüm: $Version" -ForegroundColor DarkGray

dotnet publish $projectFullPath `
	-c $Configuration `
	-r $Runtime `
	--self-contained true `
	/p:PublishSingleFile=false `
	/p:PublishReadyToRun=true `
	/p:Version=$Version `
	-o $publishFullPath

Write-Host "Publish tamamlandı: $publishFullPath" -ForegroundColor Green

if (-not [string]::IsNullOrWhiteSpace($UpdateBaseUrl)) {
	$manifestUrl = $UpdateBaseUrl.TrimEnd('/') + "/latest.json"
	$bundledUpdateSettings = [ordered]@{
		manifestUrl = $manifestUrl
	} | ConvertTo-Json
	Set-Content -Path $bundledUpdateSettingsPath -Value $bundledUpdateSettings -Encoding UTF8
	Write-Host "Update ayarı yazıldı: $bundledUpdateSettingsPath" -ForegroundColor Green
}

if (-not $BuildInstaller) {
	Write-Host "Installer derlenmedi. Inno Setup ile oluşturmak için -BuildInstaller kullanın." -ForegroundColor Yellow
	exit 0
}

$innoCompiler = @(
	"${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
	"${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
	"${env:LOCALAPPDATA}\Programs\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $innoCompiler) {
	throw "Inno Setup Compiler (ISCC.exe) bulunamadı. Lütfen Inno Setup 6 yükleyin veya scripti -BuildInstaller olmadan çalıştırın."
}

$issPath = Join-Path $scriptRoot "Örnek.iss"

Write-Host "Installer derleniyor..." -ForegroundColor Cyan
& $innoCompiler $issPath "/DMyAppPublishDir=$publishFullPath" "/DMyAppVersion=$Version" "/DMyAppSetupOutDir=$installerOutputDir"

$setupFileName = "Rechnung-Setup-$Version.exe"
$downloadUrl = if ([string]::IsNullOrWhiteSpace($UpdateBaseUrl)) { $setupFileName } else { ($UpdateBaseUrl.TrimEnd('/') + '/' + $setupFileName) }
$manifest = [ordered]@{
	version = $Version
	downloadUrl = $downloadUrl
	notes = "Rechnung sürüm $Version paketi."
} | ConvertTo-Json
Set-Content -Path $updateManifestPath -Value $manifest -Encoding UTF8

Write-Host "Installer oluşturuldu: $installerOutputDir" -ForegroundColor Green
Write-Host "Update manifest oluşturuldu: $updateManifestPath" -ForegroundColor Green
