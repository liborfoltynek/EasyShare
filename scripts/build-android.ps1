# Build script for EasyShare.Android
# Builds and signs the .NET MAUI Android application
#
# Usage:
#   .\scripts\build-android.ps1                         # interactive (prompts for password)
#   .\scripts\build-android.ps1 -KeyAlias "easyshare"   # specify alias, prompt for password
param(
    [string]$Configuration = "Release",
    [string]$OutputDir = "$PSScriptRoot\..\build\android",
    [string]$KeystorePath = "d:\Projects\2026\EasyShare\easyshare.keystore",
    [string]$KeyAlias = "easyshare"
)

$ErrorActionPreference = "Stop"
$projectDir = "$PSScriptRoot\..\src\EasyShare.Android"
$projectFile = Join-Path $projectDir "EasyShare.Android.csproj"

Write-Host "EasyShare.Android - Build & Sign" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration"
Write-Host "Output:        $OutputDir"
Write-Host ""

# ── Keystore verification ──────────────────────────────────────────
if (-not (Test-Path $KeystorePath)) {
    Write-Host "Keystore not found: $KeystorePath" -ForegroundColor Red
    Write-Host "Create one with: keytool -genkeypair -v -keystore easyshare.keystore -alias easyshare -keyalg RSA -keysize 2048 -validity 10000" -ForegroundColor Yellow
    exit 1
}

Write-Host "Keystore:      $KeystorePath" -ForegroundColor Green
Write-Host "Key alias:     $KeyAlias" -ForegroundColor Green
Write-Host ""

# ── Prompt for password (never stored) ─────────────────────────────
$securePass = Read-Host "Enter keystore password" -AsSecureString
$keystorePass = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePass))

if ([string]::IsNullOrWhiteSpace($keystorePass)) {
    Write-Host "Password cannot be empty." -ForegroundColor Red
    exit 1
}

# ── Build ──────────────────────────────────────────────────────────
Write-Host ""
Write-Host "Building..." -ForegroundColor Cyan

dotnet publish $projectFile `
    -f net10.0-android `
    -c $Configuration `
    -o $OutputDir `
    /p:AndroidKeyStore=true `
    /p:AndroidSigningKeyStore="$KeystorePath" `
    /p:AndroidSigningKeyAlias="$KeyAlias" `
    /p:AndroidSigningStorePass="$keystorePass" `
    /p:AndroidSigningKeyPass="$keystorePass"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# ── Cleanup password from memory ──────────────────────────────────
$keystorePass = $null
[GC]::Collect()

Write-Host ""
Write-Host "Build complete. Signed APK/AAB files are in: $OutputDir" -ForegroundColor Cyan
Write-Host ""

# Show output files
Get-ChildItem $OutputDir -Filter "*.apk" -Recurse | ForEach-Object {
    Write-Host "  APK: $($_.FullName)" -ForegroundColor Green
}
Get-ChildItem $OutputDir -Filter "*.aab" -Recurse | ForEach-Object {
    Write-Host "  AAB: $($_.FullName)" -ForegroundColor Green
}
