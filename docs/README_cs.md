# EasyShare – Dokumentace

Systém pro snadné sdílení souborů přes web s krátkými URL. Podporuje jednotlivé soubory, adresáře i ZIP archivy.

## Komponenty

| Komponenta | Popis |
|------------|-------|
| **EasyShare.Server** | PHP webové rozhraní + upload API (IIS nebo Apache) |
| **EasyShare.Windows** | Windows konzolová aplikace (.NET 10) |
| **EasyShare.Android** | Android aplikace (.NET MAUI) |

## Nasazení webové části (PHP)

### Požadavky

- PHP 7.4+ s GD rozšířením (pro OG obrázky)
- IIS s URL Rewrite modulem nebo Apache s mod_rewrite
- Zápis do datového adresáře

### Postup

1. **Zkopírujte soubory** ze složky `src/EasyShare.Server/` na webový server:
   - `index.php`, `upload.php`, `og_icon.php`, `setup.php`
   - `config.php`, `lang.php`
   - `config.example.json` (přejmenujte na `config.json` nebo použijte průvodce)
   - `web.config` (pro IIS) nebo `.htaccess` (pro Apache)
   - `lang/` adresář s překlady

2. **Vytvořte datový adresář** se zápisovými právy.

3. **Spusťte průvodce** — otevřete web v prohlížeči. Pokud `config.json` neexistuje nebo je prázdný, automaticky se zobrazí průvodce konfigurací (`setup.php`).

4. **Vyplňte konfiguraci:**
   - **Cesta k datovému adresáři** — absolutní cesta na serveru (např. `C:/web/mysite/data`)
   - **Veřejná URL** — bez lomítka na konci (např. `https://files.example.com`)
   - **API klíč** — tajný řetězec pro ověření uploadů (tlačítko vygeneruje náhodný)
   - **Délka klíče** — počet znaků v URL identifikátoru (výchozí: 8)

5. **IIS** — přiložený `web.config` obsahuje pravidla pro URL rewrite.

6. **Apache** — přiložený `.htaccess` soubor obsahuje pravidla pro přepisování URL. Ujistěte se, že je povolen `mod_rewrite` a v konfiguraci VirtualHost je nastaven `AllowOverride All`.

### Konfigurace (`config.json`)

```json
{
    "DataDir": "C:/web/mysite/data",
    "BaseUrl": "https://files.example.com",
    "ApiKey": "váš-tajný-klíč",
    "MaxFileSize": 1073741824,
    "ShareCodeLength": 8
}
```

> **⚠️ Důležité:** `ShareCodeLength` musí být stejný ve všech klientech (web, Windows app, Android app). Je to odpovědnost uživatele.

### Lokalizace

Systém podporuje více jazyků. Překlady jsou v `lang/*.json`. Přidání nového jazyka:

1. Zkopírujte `lang/en.json` jako `lang/de.json` (nebo jiný kód)
2. Přeložte hodnoty
3. Systém automaticky detekuje nový jazyk

Detekce jazyka: `?lang=` parametr → cookie → `Accept-Language` header → výchozí `en`.

---

## Windows aplikace (EasyShare.Windows)

### Sestavení

```bash
cd src/EasyShare.Windows
dotnet build
dotnet publish -c Release -r win-x64 --self-contained
```

Nebo použijte build skript:

```powershell
.\scripts\build-windows.ps1
```

### První spuštění

Při prvním spuštění (bez `config.json`) se zobrazí průvodce konfigurací v konzoli. Případně spusťte:

```bash
EasyShare.exe setup
```

Aplikace automaticky detekuje jazyk systému a zobrazí české nebo anglické rozhraní.

### Konfigurace (`config.json`)

```json
{
    "BaseUrl": "https://files.example.com",
    "UploadEndpoint": "https://files.example.com/upload.php",
    "ApiKey": "váš-tajný-klíč",
    "ShareCodeLength": 8,
    "DataRoot": "\\\\server\\web\\data",
    "SmbPort": 445,
    "SmbTimeoutMs": 1500
}
```

- **UploadEndpoint** — celá URL upload endpointu
- **DataRoot** — UNC cesta pro přímý SMB přenos (volitelné, rychlejší v LAN)
- Pokud DataRoot není vyplněn, použije se pouze HTTP upload

### Použití

```bash
EasyShare.exe file C:\soubor.pdf                 # sdílet soubor
EasyShare.exe file C:\soubor.pdf 7d              # s platností 7 dní
EasyShare.exe dir C:\adresar                     # sdílet adresář
EasyShare.exe zip C:\adresar 2026-12-31          # ZIP s datem expirace
EasyShare.exe clipboard                          # obsah schránky
EasyShare.exe import config.json                 # importovat konfiguraci
EasyShare.exe setup                              # rekonfigurace
```

---

## Android aplikace (EasyShare.Android)

### Sestavení

```bash
cd src/EasyShare.Android
dotnet build -f net10.0-android
dotnet publish -f net10.0-android -c Release
```

Nebo použijte build skript:

```powershell
.\scripts\build-android.ps1
```

APK bude v `bin/Release/net10.0-android/publish/`.

### První spuštění

Při prvním spuštění se zobrazí průvodce konfigurací:

1. **Upload Endpoint** — celá URL (např. `https://files.example.com/upload.php`)
2. **API klíč** — stejný jako v konfiguraci serveru
3. **Délka klíče** — musí odpovídat nastavení serveru

Nastavení lze kdykoli změnit přes ikonu ⚙️ v hlavičce aplikace.

Aplikace automaticky detekuje jazyk zařízení a zobrazí české nebo anglické rozhraní.

### Funkce

- Sdílení souborů přes Android "Share" intent
- Výběr souboru z aplikace
- Volba platnosti odkazu
- Automatické kopírování URL do schránky
- Import/export konfigurace

---

## Bezpečnost

- URL klíče poskytují **security through obscurity** — nejsou heslem, ale pouhé uhádnutí je nepravděpodobné
- API klíč chrání upload endpoint před neautorizovaným přístupem
- `config.json` obsahuje citlivé údaje — **nepřidávejte do gitu**
- Datový adresář by neměl být přímo přístupný z webu (soubory se servírují přes PHP)
