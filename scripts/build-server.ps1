# Build script for EasyShare.Server
# Copies PHP files to an output directory for deployment
param(
    [string]$OutputDir = "$PSScriptRoot\..\build\server"
)

$ErrorActionPreference = "Stop"
$srcDir = "$PSScriptRoot\..\src\EasyShare.Server"

Write-Host "EasyShare.Server - Build" -ForegroundColor Cyan
Write-Host "Source:  $srcDir"
Write-Host "Output:  $OutputDir"
Write-Host ""

if (Test-Path $OutputDir) {
    Remove-Item $OutputDir -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

# Copy PHP files
$files = @(
    "index.php", "upload.php", "config.php", "lang.php",
    "og_icon.php", "setup.php", "config.example.json",
    "web.config", ".htaccess", ".user.ini"
)
foreach ($file in $files) {
    $src = Join-Path $srcDir $file
    if (Test-Path $src) {
        Copy-Item $src $OutputDir
        Write-Host "  Copied: $file" -ForegroundColor Green
    }
}

# Copy lang directory
$langSrc = Join-Path $srcDir "lang"
if (Test-Path $langSrc) {
    Copy-Item $langSrc -Destination (Join-Path $OutputDir "lang") -Recurse
    Write-Host "  Copied: lang/" -ForegroundColor Green
}

# Create empty data directory
New-Item -ItemType Directory -Path (Join-Path $OutputDir "data") -Force | Out-Null
Write-Host "  Created: data/" -ForegroundColor Green

Write-Host ""
Write-Host "Build complete. Deploy contents of '$OutputDir' to your web server." -ForegroundColor Cyan
