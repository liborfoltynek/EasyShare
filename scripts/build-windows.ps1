# Build script for EasyShare.Windows
# Builds the .NET console application as a self-contained executable
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$OutputDir = "$PSScriptRoot\..\build\windows"
)

$ErrorActionPreference = "Stop"
$projectDir = "$PSScriptRoot\..\src\EasyShare.Windows"
$projectFile = Join-Path $projectDir "EasyShare.Windows.csproj"

Write-Host "EasyShare.Windows - Build" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration"
Write-Host "Runtime:       $Runtime"
Write-Host "Output:        $OutputDir"
Write-Host ""

dotnet publish $projectFile `
    -c $Configuration `
    -r $Runtime `
    --self-contained `
    -o $OutputDir

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Build complete: $OutputDir\EasyShare.exe" -ForegroundColor Cyan
