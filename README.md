# EasyShare

A self-hosted file sharing system with short URLs. Share files, directories, and ZIP archives through a simple web interface with Windows and Android clients.

📖 **[English Documentation](docs/README_en.md)** · 📖 **[Česká dokumentace](docs/README_cs.md)**

## Features

- **Short share URLs** — files accessible via `https://your-server.com/abcDEF12`
- **Multiple share types** — single file, browsable directory, ZIP archive
- **Image previews** — inline preview with lightbox, Open Graph meta tags for social media
- **Link expiration** — optional time-limited shares (hours, days, specific date)
- **Multi-language web UI** — Czech, English (auto-detected from browser)
- **Setup wizard** — browser-based first-run configuration
- **Dual upload methods** — direct SMB (LAN) with HTTP fallback

## Components

| Component | Technology | Description |
|-----------|-----------|-------------|
| **EasyShare.Server** | PHP 7.4+ | Web frontend, download pages, upload API, setup wizard |
| **EasyShare.Windows** | .NET 10 (C#) | Windows console uploader with SMB/HTTP support |
| **EasyShare.Android** | .NET MAUI | Android sharing app with file picker |

## Quick Start

1. Deploy `src/EasyShare.Server/` to your web server (IIS or Apache)
2. Open the site in a browser — the setup wizard guides you through configuration
3. Download client configuration from the setup page
4. Use the Windows or Android client to upload and share files

## Building

### Server (PHP)

No build step required — deploy PHP files directly. See [detailed docs](docs/README_en.md).

### Windows Console App

```powershell
.\scripts\build-windows.ps1
```

Or manually:

```bash
cd src/EasyShare.Windows
dotnet publish -c Release -r win-x64 --self-contained
```

### Android App

```powershell
.\scripts\build-android.ps1
```

Or manually:

```bash
cd src/EasyShare.Android
dotnet publish -f net10.0-android -c Release
```

## Project Structure

```
EasyShare/
├── src/
│   ├── EasyShare.Server/      # PHP web backend
│   ├── EasyShare.Windows/     # Windows console uploader
│   └── EasyShare.Android/     # Android MAUI app
├── docs/                      # Documentation (EN + CS)
├── scripts/                   # Build scripts
├── EasyShare.sln              # Visual Studio solution
└── LICENSE
```

## License

Free to use, modify, and distribute. See [LICENSE](LICENSE) for details.
