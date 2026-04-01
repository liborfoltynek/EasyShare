# Build script for EasyShare.Android
# Builds the .NET MAUI Android application
param(
    [string]$Configuration = "Release",
    [string]$OutputDir = "$PSScriptRoot\..\build\android"
)

$ErrorActionPreference = "Stop"
$projectDir = "$PSScriptRoot\..\src\EasyShare.Android"
$projectFile = Join-Path $projectDir "EasyShare.Android.csproj"

Write-Host "EasyShare.Android - Build" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration"
Write-Host "Output:        $OutputDir"
Write-Host ""

dotnet publish $projectFile `
    -f net10.0-android `
    -c $Configuration `
    -o $OutputDir

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Build complete. APK files are in: $OutputDir" -ForegroundColor Cyan
