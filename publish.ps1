param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$project = Join-Path $PSScriptRoot "Örnek\Örnek.csproj"
$outDir = Join-Path $PSScriptRoot "publish\$Runtime"

Write-Host "Publishing $project -> $outDir" -ForegroundColor Cyan

& dotnet publish $project -c $Configuration -r $Runtime --self-contained true -o $outDir /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true

Write-Host "Done. Run: $outDir\Örnek.exe" -ForegroundColor Green
