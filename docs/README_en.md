# EasyShare – Documentation

A system for easy file sharing via the web with short URLs. Supports single files, directories, and ZIP archives.

## Components

| Component | Description |
|-----------|-------------|
| **EasyShare.Server** | PHP web interface + upload API (IIS or Apache) |
| **EasyShare.Windows** | Windows console application (.NET 10) |
| **EasyShare.Android** | Android application (.NET MAUI) |

## Web Deployment (PHP)

### Requirements

- PHP 7.4+ with GD extension (for OG images)
- IIS with URL Rewrite module or Apache with mod_rewrite
- Write access to data directory

### Steps

1. **Copy files** from `src/EasyShare.Server/` to your web server:
   - `index.php`, `upload.php`, `og_icon.php`, `setup.php`
   - `config.php`, `lang.php`
   - `config.example.json` (rename to `config.json` or use the wizard)
   - `web.config` (for IIS) or `.htaccess` (for Apache)
   - `lang/` directory with translations

2. **Create data directory** with write permissions.

3. **Run the wizard** — open the website in a browser. If `config.json` doesn't exist or is empty, the configuration wizard (`setup.php`) will be displayed automatically.

4. **Fill in the configuration:**
   - **Data directory path** — absolute server path (e.g., `/var/www/mysite/data`)
   - **Public URL** — without trailing slash (e.g., `https://files.example.com`)
   - **API key** — secret string for upload authentication (button generates a random one)
   - **Key length** — number of characters in URL identifier (default: 8)

5. **IIS** — the included `web.config` contains the URL rewrite rules.

6. **Apache** — the included `.htaccess` file contains the rewrite rules. Make sure `mod_rewrite` is enabled and `AllowOverride All` is set in your VirtualHost.

### Configuration (`config.json`)

```json
{
    "DataDir": "/var/www/mysite/data",
    "BaseUrl": "https://files.example.com",
    "ApiKey": "your-secret-key",
    "MaxFileSize": 1073741824,
    "ShareCodeLength": 8
}
```

> **⚠️ Important:** `ShareCodeLength` must be the same across all clients (web, Windows app, Android app). This is the user's responsibility.

### Localization

The system supports multiple languages. Translations are in `lang/*.json`. Adding a new language:

1. Copy `lang/en.json` as `lang/de.json` (or another code)
2. Translate the values
3. The system automatically detects the new language

Language detection: `?lang=` parameter → cookie → `Accept-Language` header → fallback `en`.

---

## Windows Application (EasyShare.Windows)

### Building

```bash
cd src/EasyShare.Windows
dotnet build
dotnet publish -c Release -r win-x64 --self-contained
```

Or use the build script:

```powershell
.\scripts\build-windows.ps1
```

### First Run

On first run (without `config.json`), the console configuration wizard will appear. Alternatively, run:

```bash
EasyShare.exe setup
```

The application automatically detects the system language and shows Czech or English UI accordingly.

### Configuration (`config.json`)

```json
{
    "BaseUrl": "https://files.example.com",
    "UploadEndpoint": "https://files.example.com/upload.php",
    "ApiKey": "your-secret-key",
    "ShareCodeLength": 8,
    "DataRoot": "\\\\server\\web\\data",
    "SmbPort": 445,
    "SmbTimeoutMs": 1500
}
```

- **UploadEndpoint** — full upload endpoint URL
- **DataRoot** — UNC path for direct SMB transfer (optional, faster on LAN)
- If DataRoot is not set, only HTTP upload is used

### Usage

```bash
EasyShare.exe file C:\file.pdf                  # share a file
EasyShare.exe file C:\file.pdf 7d               # with 7-day expiration
EasyShare.exe dir C:\directory                   # share a directory
EasyShare.exe zip C:\directory 2026-12-31        # ZIP with expiration date
EasyShare.exe clipboard                          # clipboard content
EasyShare.exe import config.json                 # import configuration
EasyShare.exe setup                              # reconfigure
```

---

## Android Application (EasyShare.Android)

### Building

```bash
cd src/EasyShare.Android
dotnet build -f net10.0-android
dotnet publish -f net10.0-android -c Release
```

Or use the build script:

```powershell
.\scripts\build-android.ps1
```

The APK will be in `bin/Release/net10.0-android/publish/`.

### First Run

On first launch, a configuration wizard will appear:

1. **Upload Endpoint** — full URL (e.g., `https://files.example.com/upload.php`)
2. **API key** — same as in the server configuration
3. **Key length** — must match the server setting

Settings can be changed anytime via the ⚙️ icon in the app header.

The application automatically detects the device language and shows Czech or English UI accordingly.

### Features

- File sharing via Android "Share" intent
- File picker from within the app
- Link expiration selection
- Automatic URL copy to clipboard
- Configuration import/export

---

## Security

- URL keys provide **security through obscurity** — they are not passwords, but guessing them is unlikely
- API key protects the upload endpoint from unauthorized access
- `config.json` contains sensitive data — **do not add to git**
- The data directory should not be directly accessible from the web (files are served through PHP)
