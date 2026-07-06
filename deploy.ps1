# YouDownloader Deployment Script
# Targets Windows x64 self-contained release

$ErrorActionPreference = "Stop"

Write-Host "==============================================" -ForegroundColor Cyan
Write-Host "   Deploying YouDownloader (.NET 10 win-x64)   " -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan

$projectPath = ".\YouDownloader\YouDownloader.csproj"
$publishDir = ".\publish"
$zipFile = ".\YouDownloader_Release_win-x64.zip"

# 1. Clean previous build artifacts
if (Test-Path $publishDir) {
    Write-Host "Cleaning previous publish directory..." -ForegroundColor Gray
    Remove-Item -Recurse -Force $publishDir
}
if (Test-Path $zipFile) {
    Write-Host "Cleaning previous release zip..." -ForegroundColor Gray
    Remove-Item -Force $zipFile
}

# 2. Run Dotnet Publish
Write-Host "Publishing self-contained release..." -ForegroundColor Yellow
dotnet publish $projectPath `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishReadyToRun=true `
    -o $publishDir

if ($LASTEXITCODE -ne 0) {
    Write-Error "Dotnet publish failed!"
    exit $LASTEXITCODE
}

# 3. Create ZIP Archive
Write-Host "Packaging files into $zipFile..." -ForegroundColor Yellow
Compress-Archive -Path "$publishDir\*" -DestinationPath $zipFile -Force

Write-Host "==============================================" -ForegroundColor Green
Write-Host "   Deployment successfully packaged!          " -ForegroundColor Green
Write-Host "   Release: $zipFile" -ForegroundColor Green
Write-Host "==============================================" -ForegroundColor Green
