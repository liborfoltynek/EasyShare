using System.Globalization;

namespace EasyShare.Android;

/// <summary>
/// Simple localization helper — auto-detects Czech vs English from device culture.
/// </summary>
public static class Strings
{
    private static bool IsCzech =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "cs";

    public static string L(string en, string cs) => IsCzech ? cs : en;

    // App title
    public static string AppTitle => "EasyShare";

    // Main page
    public static string SelectedFile => L("Selected file", "Vybraný soubor");
    public static string LinkExpiry => L("Link expiry", "Platnost odkazu");
    public static string NoLimit => L("No limit", "Bez omezení");
    public static string OneHour => L("1 hour", "1 hodina");
    public static string TwentyFourHours => L("24 hours", "24 hodin");
    public static string SevenDays => L("7 days", "7 dní");
    public static string ThirtyDays => L("30 days", "30 dní");
    public static string Uploading => L("Uploading...", "Nahrávání...");
    public static string Uploaded => L("Uploaded!", "Nahráno!");
    public static string ShareLink => L("Share link:", "Odkaz pro sdílení:");
    public static string CopiedToClipboard => L("Link copied to clipboard", "Odkaz zkopírován do schránky");
    public static string Copy => L("Copy", "Kopírovat");
    public static string ShareUrl => L("Share URL", "Sdílet URL");
    public static string PickFile => L("Pick file", "Vybrat soubor");
    public static string UploadAndShare => L("Upload & share", "Nahrát a sdílet");
    public static string NewUpload => L("New upload", "Nový upload");
    public static string ServerNotSet => L("Server not configured", "Server nenastaven");
    public static string Error => L("Error", "Chyba");
    public static string ServerNotConfigured => L("Server is not configured. Open settings (⚙️).", "Server není nakonfigurován. Otevřete nastavení (⚙️).");
    public static string NoFileSelected => L("No file selected", "Žádný soubor nebyl vybrán");
    public static string UnknownError => L("Unknown error", "Neznámá chyba");
    public static string UploadCancelled => L("Upload cancelled", "Nahrávání zrušeno");
    public static string UploadError(string msg) => L($"Upload error: {msg}", $"Chyba při nahrávání: {msg}");
    public static string CannotPickFile(string msg) => L($"Cannot pick file: {msg}", $"Nelze vybrat soubor: {msg}");
    public static string SizeUnknown => L("Size unknown", "Velikost neznámá");
    public static string ValidUntil(string date) => L($"Valid until: {date}", $"Platnost do: {date}");
    public static string PickFileTitle => L("Select file to share", "Vyberte soubor ke sdílení");
    public static string ShareTitle => L("Share link", "Sdílet odkaz");

    // Settings page
    public static string Settings => L("⚙️ Settings", "⚙️ Nastavení");
    public static string SettingsTitle => L("Settings", "Nastavení");
    public static string ApiKeyLabel => L("API key", "API klíč");
    public static string CodeLengthLabel => L("URL code length", "Délka kódu v URL");
    public static string CodeLengthHint => L("Must be the same across all clients.", "Musí být stejná ve všech klientech.");
    public static string Save => L("Save", "Uložit");
    public static string Back => L("Back", "Zpět");
    public static string BackupRestore => L("📲 Backup & Restore", "📲 Záloha a obnova");
    public static string BackupRestoreDesc => L(
        "Import configuration downloaded from the server (setup.php) or export current settings for transfer to another device.",
        "Importujte konfiguraci staženou ze serveru (setup.php) nebo exportujte aktuální nastavení pro přenos na jiné zařízení.");
    public static string Import => L("⬇ Import", "⬇ Import");
    public static string Export => L("⬆ Export", "⬆ Export");
    public static string FillEndpoint => L("Fill in Upload Endpoint.", "Vyplňte Upload Endpoint.");
    public static string InvalidUrl => L("Upload Endpoint must be a valid URL.", "Upload Endpoint musí být platná URL adresa.");
    public static string FillApiKey => L("Fill in API key.", "Vyplňte API klíč.");
    public static string InvalidCodeLength => L("Code length must be a number from 4 to 32.", "Délka kódu musí být číslo od 4 do 32.");
    public static string SettingsSaved => L("✓ Settings saved.", "✓ Nastavení uloženo.");
    public static string ImportPickTitle => L("Select configuration file (JSON)", "Vyberte konfigurační soubor (JSON)");
    public static string InvalidConfigFile => L("File does not contain valid configuration.", "Soubor neobsahuje platnou konfiguraci.");
    public static string Imported(int count) => L($"✓ Imported ({count} values).", $"✓ Importováno ({count} hodnot).");
    public static string ImportError(string msg) => L($"Import error: {msg}", $"Chyba importu: {msg}");
    public static string FillConfigFirst => L("Fill in and save configuration first.", "Nejdříve vyplňte a uložte konfiguraci.");
    public static string ExportTitle => L("Export configuration", "Export konfigurace");
    public static string ExportError(string msg) => L($"Export error: {msg}", $"Chyba exportu: {msg}");
    public static string InvalidServerResponse => L("Invalid server response", "Neplatná odpověď serveru");

    // Setup page
    public static string SetupServerTitle => L("Server setup", "Nastavení serveru");
    public static string SetupDesc => L("To use the app, fill in your server details.", "Pro používání aplikace vyplňte údaje o vašem serveru.");
    public static string EndpointHelp => L("Full URL of the upload endpoint on your server.", "Celá URL adresa upload endpointu na vašem serveru.");
    public static string ApiKeyHelp => L("Same key as in the server configuration.", "Stejný klíč jako v konfiguraci serveru.");
    public static string CodeLengthHelp => L("Must be the same across all clients (default: 8).", "Musí být stejná ve všech klientech (výchozí: 8).");
    public static string SaveAndContinue => L("Save & continue", "Uložit a pokračovat");

    // Setup page — import section
    public static string SetupOrImport => L("— or import configuration —", "— nebo importujte konfiguraci —");
    public static string ImportFromFile => L("📂 Import from file", "📂 Import ze souboru");
    public static string PasteJsonHint => L("Or paste JSON configuration here:", "Nebo sem vložte JSON konfiguraci:");
    public static string PasteJsonPlaceholder => L("{\"UploadEndpoint\":\"...\",\"ApiKey\":\"...\",\"ShareCodeLength\":8}", "{\"UploadEndpoint\":\"...\",\"ApiKey\":\"...\",\"ShareCodeLength\":8}");
    public static string ApplyPastedJson => L("Apply pasted configuration", "Použít vloženou konfiguraci");
    public static string InvalidJson => L("Invalid JSON format. Check the pasted text.", "Neplatný formát JSON. Zkontrolujte vložený text.");
    public static string PasteEmpty => L("Paste or type a JSON configuration first.", "Nejdříve vložte nebo napište JSON konfiguraci.");
}
