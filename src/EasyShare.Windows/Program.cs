using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace EasyShare.Windows;

class Program
{
    // ════════════════════════════════════════════════════════════════════
    //  Localization — auto-detect Czech vs English based on OS culture
    // ════════════════════════════════════════════════════════════════════

    static bool IsCzech => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "cs";

    static string L(string en, string cs) => IsCzech ? cs : en;

    // ════════════════════════════════════════════════════════════════════
    //  Configuration (loaded from config.json)
    // ════════════════════════════════════════════════════════════════════

    class AppConfig
    {
        public string DataRoot { get; set; } = "";
        public string BaseUrl { get; set; } = "";
        public string UploadEndpoint { get; set; } = "";
        public string ApiKey { get; set; } = "";
        public int ShareCodeLength { get; set; } = 8;
        public int SmbPort { get; set; } = 445;
        public int SmbTimeoutMs { get; set; } = 1500;
    }

    static AppConfig Config = new();

    static string ConfigFilePath =>
        Path.Combine(AppContext.BaseDirectory, "config.json");

    /// <summary>
    /// Loads configuration from config.json next to the executable.
    /// Returns false if config file doesn't exist or required fields are empty.
    /// </summary>
    static bool LoadConfig()
    {
        if (!File.Exists(ConfigFilePath))
        {
            return false;
        }

        try
        {
            var json = File.ReadAllText(ConfigFilePath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            Config = JsonSerializer.Deserialize<AppConfig>(json, options) ?? new AppConfig();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if the configuration has enough data to do anything useful.
    /// </summary>
    static bool IsConfigured()
    {
        // At least one upload method must be configured
        bool hasSmbConfig = !string.IsNullOrWhiteSpace(Config.DataRoot)
                         && !string.IsNullOrWhiteSpace(Config.BaseUrl);
        bool hasHttpConfig = !string.IsNullOrWhiteSpace(Config.UploadEndpoint)
                          && !string.IsNullOrWhiteSpace(Config.ApiKey);
        return hasSmbConfig || hasHttpConfig;
    }

    /// <summary>
    /// Whether SMB upload is configured (DataRoot + BaseUrl present).
    /// </summary>
    static bool IsSmbConfigured =>
        !string.IsNullOrWhiteSpace(Config.DataRoot)
        && !string.IsNullOrWhiteSpace(Config.BaseUrl);

    /// <summary>
    /// Whether HTTP upload is configured (UploadEndpoint + ApiKey present).
    /// </summary>
    static bool IsHttpConfigured =>
        !string.IsNullOrWhiteSpace(Config.UploadEndpoint)
        && !string.IsNullOrWhiteSpace(Config.ApiKey);

    /// <summary>
    /// Extract SMB hostname from UNC path. E.g. \\server\share -> server
    /// </summary>
    static string? ExtractSmbHost()
    {
        if (string.IsNullOrWhiteSpace(Config.DataRoot)) {
            return null;
        }
        var path = Config.DataRoot;
        if (path.StartsWith(@"\\"))
        {
            var parts = path.Substring(2).Split(new[] { '\\', '/' }, 2);
            if (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]))
            {
                return parts[0];
            }
        }
        return null;
    }

    /// <summary>
    /// Returns true if SMB share is accessible.
    /// </summary>
    static bool IsSmbAvailable()
    {
        if (!IsSmbConfigured) {
            return false;
        }

        var smbHost = ExtractSmbHost();
        if (smbHost == null)
        {
            // Local path — just check directory
            return Directory.Exists(Config.DataRoot);
        }

        try
        {
            using var tcp = new System.Net.Sockets.TcpClient();
            var connectTask = tcp.ConnectAsync(smbHost, Config.SmbPort);
            if (!connectTask.Wait(Config.SmbTimeoutMs))
            {
                return false;
            }

            if (!tcp.Connected)
            {
                return false;
            }

            return Directory.Exists(Config.DataRoot);
        }
        catch
        {
            return false;
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Setup wizard (first-run)
    // ════════════════════════════════════════════════════════════════════

    static int RunSetupWizard()
    {
        Console.WriteLine();
        Console.WriteLine("╔══════════════════════════════════════════════════╗");
        Console.WriteLine(L("║     EasyShare – Configuration Wizard            ║",
                            "║     EasyShare – Průvodce konfigurací            ║"));
        Console.WriteLine("╚══════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine(L("Configuration not found. Enter connection details.",
                            "Konfigurace nebyla nalezena. Vyplňte údaje pro připojení k serveru."));
        Console.WriteLine(L("Leave a field empty to skip (press Enter).",
                            "Ponechte pole prázdné pro přeskočení (stiskněte Enter)."));
        Console.WriteLine();

        Console.Write(L("BaseUrl (public site URL, e.g. https://files.example.com): ",
                        "BaseUrl (veřejná URL webu, např. https://files.example.com): "));
        var baseUrl = Console.ReadLine()?.Trim() ?? "";

        Console.Write(L("UploadEndpoint (full upload URL, e.g. https://files.example.com/upload.php): ",
                        "UploadEndpoint (celá URL pro upload, např. https://files.example.com/upload.php): "));
        var uploadEndpoint = Console.ReadLine()?.Trim() ?? "";

        Console.Write(L("ApiKey (secret key for upload authentication): ",
                        "ApiKey (tajný klíč pro ověření uploadů): "));
        var apiKey = Console.ReadLine()?.Trim() ?? "";

        Console.Write(L($"ShareCodeLength (URL code length, default 8): ",
                        $"ShareCodeLength (délka kódu v URL, výchozí 8): "));
        var codeLenStr = Console.ReadLine()?.Trim() ?? "";
        int codeLength = 8;
        if (!string.IsNullOrEmpty(codeLenStr) && int.TryParse(codeLenStr, out int cl) && cl >= 4 && cl <= 32)
        {
            codeLength = cl;
        }

        Console.Write(L("DataRoot (network/local path for SMB, e.g. \\\\server\\web\\data, or empty): ",
                        "DataRoot (síťová/lokální cesta pro SMB, např. \\\\server\\web\\data, nebo prázdné): "));
        var dataRoot = Console.ReadLine()?.Trim() ?? "";

        Console.Write(L($"SmbPort (default 445): ",
                        $"SmbPort (výchozí 445): "));
        var smbPortStr = Console.ReadLine()?.Trim() ?? "";
        int smbPort = 445;
        if (!string.IsNullOrEmpty(smbPortStr) && int.TryParse(smbPortStr, out int sp))
        {
            smbPort = sp;
        }

        Console.Write(L($"SmbTimeoutMs (default 1500): ",
                        $"SmbTimeoutMs (výchozí 1500): "));
        var smbTimeoutStr = Console.ReadLine()?.Trim() ?? "";
        int smbTimeout = 1500;
        if (!string.IsNullOrEmpty(smbTimeoutStr) && int.TryParse(smbTimeoutStr, out int st))
        {
            smbTimeout = st;
        }

        var config = new AppConfig
        {
            BaseUrl = baseUrl.TrimEnd('/'),
            UploadEndpoint = uploadEndpoint,
            ApiKey = apiKey,
            ShareCodeLength = codeLength,
            DataRoot = dataRoot,
            SmbPort = smbPort,
            SmbTimeoutMs = smbTimeout,
        };

        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(config, options);
            File.WriteAllText(ConfigFilePath, json);
            Console.WriteLine();
            Console.WriteLine(L($"✓ Configuration saved to: {ConfigFilePath}",
                                $"✓ Konfigurace uložena do: {ConfigFilePath}"));
            Console.WriteLine();
            Config = config;
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(L($"Error: Cannot save configuration: {ex.Message}",
                                      $"Chyba: Nelze uložit konfiguraci: {ex.Message}"));
            return 1;
        }
    }

    [STAThread]
    static int Main(string[] args)
    {
        // Load config
        if (!LoadConfig() || !IsConfigured())
        {
            // No config.json or incomplete — run setup wizard
            if (args.Length > 0 && args[0].ToLowerInvariant() == "setup")
            {
                return RunSetupWizard();
            }

            if (!File.Exists(ConfigFilePath))
            {
                Console.Error.WriteLine(L("Configuration not found. Starting wizard...",
                                          "Konfigurace nenalezena. Spouštím průvodce..."));
                Console.Error.WriteLine();
                int setupResult = RunSetupWizard();
                if (setupResult != 0 || !IsConfigured())
                {
                    return 1;
                }

                // If no args besides setup, exit after wizard
                if (args.Length == 0)
                {
                    Console.WriteLine(L("Configuration complete. Run the program again with the desired command.",
                                        "Konfigurace dokončena. Spusťte program znovu s požadovaným příkazem."));
                    return 0;
                }
            }
            else if (!IsConfigured())
            {
                Console.Error.WriteLine(L("Error: Configuration is incomplete. Run 'EasyShare.exe setup' to reconfigure.",
                                          "Chyba: Konfigurace je neúplná. Spusťte 'EasyShare.exe setup' pro rekonfiguraci."));
                return 1;
            }
        }

        // Handle 'setup' command explicitly even when configured
        if (args.Length > 0 && args[0].ToLowerInvariant() == "setup")
        {
            return RunSetupWizard();
        }

        // Handle 'import' command
        if (args.Length >= 2 && args[0].ToLowerInvariant() == "import")
        {
            return ImportConfig(args[1]);
        }

        if (args.Length < 1)
        {
            PrintUsage();
            return 1;
        }

        string mode = args[0].ToLowerInvariant();

        // Clipboard mode needs only 1 arg (mode) + optional expiration
        if (mode.StartsWith("clip"))
        {
            string? clipExpiresArg = args.Length >= 2 ? args[1] : null;
            DateTime? clipExpiresUtc = null;
            if (clipExpiresArg != null)
            {
                clipExpiresUtc = ParseExpiration(clipExpiresArg);
                if (clipExpiresUtc == null)
                {
                    return Error(L($"Invalid expiration format: {clipExpiresArg}",
                                   $"Neplatný formát platnosti: {clipExpiresArg}"));
                }
            }
            try { return UploadClipboard(clipExpiresUtc); }
            catch (Exception ex) { Console.Error.WriteLine(L($"Error: {ex.Message}", $"Chyba: {ex.Message}")); return 1; }
        }

        if (args.Length < 2)
        {
            PrintUsage();
            return 1;
        }

        string sourcePath = args[1];
        string? expiresArg = args.Length >= 3 ? args[2] : null;

        // Parse expiration if provided
        DateTime? expiresUtc = null;
        if (expiresArg != null)
        {
            expiresUtc = ParseExpiration(expiresArg);
            if (expiresUtc == null)
            {
                return Error(L($"Invalid expiration format: {expiresArg}",
                               $"Neplatný formát platnosti: {expiresArg}"));
            }
        }

        try
        {
            return mode switch
            {
                "file" => UploadFile(sourcePath, expiresUtc),
                "dir"  => UploadDirectory(sourcePath, expiresUtc),
                "zip"  => UploadZipped(sourcePath, expiresUtc),
                _      => Error(L($"Unknown mode: {mode}", $"Neznámý režim: {mode}"))
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(L($"Error: {ex.Message}", $"Chyba: {ex.Message}"));
            return 1;
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Upload modes
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Mode: clipboard — upload clipboard content (image or text) as a temp file.
    /// </summary>
    static int UploadClipboard(DateTime? expiresUtc)
    {
        string? tempFile = null;
        try
        {
            if (Clipboard.ContainsImage())
            {
                var img = Clipboard.GetImage();
                if (img == null)
                {
                    return Error(L("Clipboard contains an image, but it could not be read.",
                                   "Schránka obsahuje obrázek, ale nepodařilo se ho přečíst."));
                }

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                tempFile = Path.Combine(Path.GetTempPath(), $"clipboard_{timestamp}.png");

                using (var bmp = new Bitmap(img))
                {
                    bmp.Save(tempFile, ImageFormat.Png);
                }

                Console.WriteLine(L($"Clipboard: image {img.Width}×{img.Height} -> {Path.GetFileName(tempFile)}",
                                    $"Schránka: obrázek {img.Width}×{img.Height} -> {Path.GetFileName(tempFile)}"));
            }
            else if (Clipboard.ContainsText())
            {
                string text = Clipboard.GetText();
                if (string.IsNullOrEmpty(text))
                {
                    return Error(L("Clipboard is empty.", "Schránka je prázdná."));
                }

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                tempFile = Path.Combine(Path.GetTempPath(), $"clipboard_{timestamp}.txt");
                File.WriteAllText(tempFile, text);

                Console.WriteLine(L($"Clipboard: text ({text.Length} chars) -> {Path.GetFileName(tempFile)}",
                                    $"Schránka: text ({text.Length} znaků) -> {Path.GetFileName(tempFile)}"));
            }
            else if (Clipboard.ContainsFileDropList())
            {
                var files = Clipboard.GetFileDropList();
                if (files == null || files.Count == 0)
                {
                    return Error(L("Clipboard contains no files.", "Schránka neobsahuje žádné soubory."));
                }

                if (files.Count == 1)
                {
                    string singleFile = files[0]!;
                    Console.WriteLine(L($"Clipboard: file {Path.GetFileName(singleFile)}",
                                        $"Schránka: soubor {Path.GetFileName(singleFile)}"));
                    return UploadFile(singleFile, expiresUtc);
                }

                return Error(L($"Clipboard contains {files.Count} files. Use 'dir' or 'zip' mode.",
                               $"Schránka obsahuje {files.Count} souborů. Použijte režim 'dir' nebo 'zip'."));
            }
            else
            {
                return Error(L("Clipboard does not contain an image, text, or files.",
                               "Schránka neobsahuje obrázek, text ani soubory."));
            }

            int result = UploadFile(tempFile, expiresUtc);
            return result;
        }
        finally
        {
            if (tempFile != null && File.Exists(tempFile))
            {
                try { File.Delete(tempFile); }
                catch { /* ignore cleanup errors */ }
            }
        }
    }

    /// <summary>
    /// Mode: file — upload a single file for direct download.
    /// </summary>
    static int UploadFile(string filePath, DateTime? expiresUtc)
    {
        if (!File.Exists(filePath))
        {
            return Error(L($"File not found: {filePath}", $"Soubor nenalezen: {filePath}"));
        }

        string fileName = Path.GetFileName(filePath);

        if (IsSmbAvailable())
        {
            Console.Write(L($"Uploading file (SMB): {fileName} ... ",
                            $"Nahrávám soubor (SMB): {fileName} ... "));
            string key = GenerateKey();
            string targetDir = Path.Combine(Config.DataRoot, key);
            Directory.CreateDirectory(targetDir);

            string targetFile = Path.Combine(targetDir, fileName);
            File.Copy(filePath, targetFile);
            Console.WriteLine("OK");

            WriteMeta(targetDir, "file", fileName, expiresUtc);
            PrintResult(key, expiresUtc);
            return 0;
        }
        else if (IsHttpConfigured)
        {
            Console.Write(L($"Uploading file (HTTP): {fileName} ... ",
                            $"Nahrávám soubor (HTTP): {fileName} ... "));
            var result = UploadFileHttp(filePath, fileName, expiresUtc).GetAwaiter().GetResult();
            return result;
        }
        else
        {
            return Error(L("No upload method available. Check configuration (SMB/HTTP).",
                           "Žádná metoda uploadu není dostupná. Zkontrolujte konfiguraci (SMB/HTTP)."));
        }
    }

    /// <summary>
    /// Mode: dir — upload a directory for browsable access.
    /// </summary>
    static int UploadDirectory(string dirPath, DateTime? expiresUtc)
    {
        if (!Directory.Exists(dirPath))
        {
            return Error(L($"Directory not found: {dirPath}", $"Adresář nenalezen: {dirPath}"));
        }

        string dirName = new DirectoryInfo(dirPath).Name;

        if (IsSmbAvailable())
        {
            Console.Write(L($"Uploading directory (SMB): {dirName} ... ",
                            $"Nahrávám adresář (SMB): {dirName} ... "));
            string key = GenerateKey();
            string targetDir = Path.Combine(Config.DataRoot, key);
            Directory.CreateDirectory(targetDir);

            CopyDirectoryRecursive(dirPath, targetDir);
            Console.WriteLine("OK");

            WriteMeta(targetDir, "dir", dirName, expiresUtc);
            PrintResult(key, expiresUtc);
            return 0;
        }
        else if (IsHttpConfigured)
        {
            // HTTP fallback: zip the directory and upload as ZIP
            Console.WriteLine(L($"SMB unavailable, uploading directory via HTTP as ZIP...",
                                $"SMB nedostupné, nahrávám adresář přes HTTP jako ZIP..."));
            string tempZip = Path.Combine(Path.GetTempPath(), $"{dirName}_{DateTime.Now:yyyyMMdd_HHmmss}.zip");
            try
            {
                Console.Write(L($"Creating ZIP: {Path.GetFileName(tempZip)} ... ",
                                $"Vytvářím ZIP: {Path.GetFileName(tempZip)} ... "));
                CreateStoreZip(dirPath, tempZip);
                Console.WriteLine("OK");

                string zipFileName = dirName + ".zip";
                Console.Write(L($"Uploading (HTTP): {zipFileName} ... ",
                                $"Nahrávám (HTTP): {zipFileName} ... "));
                var result = UploadFileHttp(tempZip, zipFileName, expiresUtc).GetAwaiter().GetResult();
                return result;
            }
            finally
            {
                if (File.Exists(tempZip))
                {
                    try { File.Delete(tempZip); }
                    catch { }
                }
            }
        }
        else
        {
            return Error(L("No upload method available. Check configuration (SMB/HTTP).",
                           "Žádná metoda uploadu není dostupná. Zkontrolujte konfiguraci (SMB/HTTP)."));
        }
    }

    /// <summary>
    /// Mode: zip — compress directory into a store-mode ZIP and upload.
    /// </summary>
    static int UploadZipped(string dirPath, DateTime? expiresUtc)
    {
        if (!Directory.Exists(dirPath))
        {
            return Error(L($"Directory not found: {dirPath}", $"Adresář nenalezen: {dirPath}"));
        }

        string dirName = new DirectoryInfo(dirPath).Name;
        string zipFileName = dirName + ".zip";

        if (IsSmbAvailable())
        {
            string key = GenerateKey();
            string targetDir = Path.Combine(Config.DataRoot, key);
            Directory.CreateDirectory(targetDir);
            string targetZip = Path.Combine(targetDir, zipFileName);

            Console.Write(L($"Creating ZIP (SMB): {zipFileName} ... ",
                            $"Vytvářím ZIP (SMB): {zipFileName} ... "));
            CreateStoreZip(dirPath, targetZip);
            Console.WriteLine("OK");

            WriteMeta(targetDir, "zip", zipFileName, expiresUtc);
            PrintResult(key, expiresUtc);
            return 0;
        }
        else if (IsHttpConfigured)
        {
            string tempZip = Path.Combine(Path.GetTempPath(), $"{dirName}_{DateTime.Now:yyyyMMdd_HHmmss}.zip");
            try
            {
                Console.Write(L($"Creating ZIP: {zipFileName} ... ",
                                $"Vytvářím ZIP: {zipFileName} ... "));
                CreateStoreZip(dirPath, tempZip);
                Console.WriteLine("OK");

                Console.Write(L($"Uploading (HTTP): {zipFileName} ... ",
                                $"Nahrávám (HTTP): {zipFileName} ... "));
                var result = UploadFileHttp(tempZip, zipFileName, expiresUtc).GetAwaiter().GetResult();
                return result;
            }
            finally
            {
                if (File.Exists(tempZip))
                {
                    try { File.Delete(tempZip); }
                    catch { }
                }
            }
        }
        else
        {
            return Error(L("No upload method available. Check configuration (SMB/HTTP).",
                           "Žádná metoda uploadu není dostupná. Zkontrolujte konfiguraci (SMB/HTTP)."));
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  HTTP upload (fallback when SMB is unavailable)
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Uploads a file via HTTP POST to the upload endpoint.
    /// Returns 0 on success, 1 on error.
    /// </summary>
    static async Task<int> UploadFileHttp(string filePath, string displayName, DateTime? expiresUtc)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromMinutes(30); // large file support

        using var form = new MultipartFormDataContent();

        // File content
        var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(fileContent, "file", displayName);

        // Expiration
        if (expiresUtc.HasValue)
        {
            form.Add(new StringContent(expiresUtc.Value.ToString("yyyy-MM-dd")), "expires");
        }

        // API key
        client.DefaultRequestHeaders.Add("X-Api-Key", Config.ApiKey);

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync(Config.UploadEndpoint, form);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine(L("ERROR", "CHYBA"));
            Console.Error.WriteLine(L($"HTTP error: {ex.Message}", $"HTTP chyba: {ex.Message}"));
            return 1;
        }

        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine(L("ERROR", "CHYBA"));
            Console.Error.WriteLine(L($"Server returned HTTP {(int)response.StatusCode}",
                                      $"Server vrátil HTTP {(int)response.StatusCode}"));
            try
            {
                var errorJson = JsonSerializer.Deserialize<JsonElement>(responseBody);
                if (errorJson.TryGetProperty("error", out var errProp))
                {
                    Console.Error.WriteLine($"  {errProp.GetString()}");
                }
            }
            catch { }
            return 1;
        }

        // Parse success response
        try
        {
            var result = JsonSerializer.Deserialize<JsonElement>(responseBody);
            bool success = result.GetProperty("success").GetBoolean();
            if (!success)
            {
                Console.WriteLine(L("ERROR", "CHYBA"));
                if (result.TryGetProperty("error", out var errProp))
                {
                    Console.Error.WriteLine($"  {errProp.GetString()}");
                }
                return 1;
            }

            string url = result.GetProperty("url").GetString()!;
            string key = result.GetProperty("key").GetString()!;
            Console.WriteLine("OK");

            Console.WriteLine();
            Console.WriteLine(L($"  Key:       {key}", $"  Klíč:      {key}"));
            Console.WriteLine($"  URL:       {url}");
            if (expiresUtc.HasValue)
            {
                Console.WriteLine(L($"  Expires:   {expiresUtc.Value:yyyy-MM-dd HH:mm:ss}",
                                    $"  Platnost:  do {expiresUtc.Value:yyyy-MM-dd HH:mm:ss}"));
            }
            else
            {
                Console.WriteLine(L($"  Expires:   unlimited", $"  Platnost:  neomezená"));
            }
            Console.WriteLine();

            // Copy URL to clipboard
            CopyToClipboard(url);

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine(L("ERROR", "CHYBA"));
            Console.Error.WriteLine(L($"Failed to parse server response: {ex.Message}",
                                      $"Nepodařilo se zpracovat odpověď serveru: {ex.Message}"));
            return 1;
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════════════════════════════════

    static string GenerateKey()
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var bytes = RandomNumberGenerator.GetBytes(Config.ShareCodeLength);
        var key = new char[Config.ShareCodeLength];
        for (int i = 0; i < Config.ShareCodeLength; i++)
        {
            key[i] = chars[bytes[i] % chars.Length];
        }
        return new string(key);
    }

    static void WriteMeta(string targetDir, string type, string originalName, DateTime? expiresUtc)
    {
        var metaDict = new Dictionary<string, object>
        {
            ["type"] = type,
            ["name"] = originalName,
            ["created"] = DateTime.Now.ToString("o")
        };
        if (expiresUtc.HasValue)
        {
            metaDict["expires"] = expiresUtc.Value.ToString("o");
        }
        string json = JsonSerializer.Serialize(metaDict, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(targetDir, "meta.json"), json);
    }

    /// <summary>
    /// Parses expiration argument. Supports:
    ///   Xd  = X days (rounded up to end of the Xth day from now)
    ///   Xh  = X hours (rounded up to end of the Xth hour)
    ///   Xm  = X minutes (rounded up to end of the Xth minute)
    ///   yyyy-mm-dd = end of that specific day
    /// </summary>
    static DateTime? ParseExpiration(string input)
    {
        var now = DateTime.Now;

        // Try relative format: Xd, Xh, Xm
        var match = Regex.Match(input, @"^(\d+)([dhm])$", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            int value = int.Parse(match.Groups[1].Value);
            char unit = char.ToLower(match.Groups[2].Value[0]);

            return unit switch
            {
                'd' => now.Date.AddDays(value).AddDays(1).AddSeconds(-1),
                'h' => now.Date.AddHours(now.Hour)
                           .AddHours(value).AddHours(1).AddSeconds(-1),
                'm' => now.Date.AddHours(now.Hour).AddMinutes(now.Minute)
                           .AddMinutes(value).AddMinutes(1).AddSeconds(-1),
                _ => null
            };
        }

        // Try absolute date: yyyy-mm-dd
        if (DateTime.TryParseExact(input, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateTime date))
        {
            return DateTime.SpecifyKind(date, DateTimeKind.Local).AddDays(1).AddSeconds(-1);
        }

        return null;
    }

    static void CopyDirectoryRecursive(string sourceDir, string targetDir)
    {
        foreach (string dir in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(sourceDir, dir);
            string newDir = Path.Combine(targetDir, relativePath);
            Directory.CreateDirectory(newDir);
        }

        foreach (string file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(sourceDir, file);
            string newFile = Path.Combine(targetDir, relativePath);
            File.Copy(file, newFile, overwrite: false);
        }
    }

    static void CreateStoreZip(string sourceDir, string zipPath)
    {
        ZipFile.CreateFromDirectory(sourceDir, zipPath, CompressionLevel.NoCompression, includeBaseDirectory: false);
    }

    static void CopyToClipboard(string text)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "clip.exe",
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var proc = System.Diagnostics.Process.Start(psi)!;
            proc.StandardInput.Write(text);
            proc.StandardInput.Close();
            proc.WaitForExit(2000);
            Console.WriteLine(L("  (URL copied to clipboard)", "  (URL zkopírována do schránky)"));
        }
        catch { }
    }

    static void PrintResult(string key, DateTime? expiresUtc)
    {
        string url = $"{Config.BaseUrl.TrimEnd('/')}/{key}";
        Console.WriteLine();
        Console.WriteLine(L($"  Key:       {key}", $"  Klíč:      {key}"));
        Console.WriteLine($"  URL:       {url}");
        if (expiresUtc.HasValue)
        {
            Console.WriteLine(L($"  Expires:   {expiresUtc.Value:yyyy-MM-dd HH:mm:ss}",
                                $"  Platnost:  do {expiresUtc.Value:yyyy-MM-dd HH:mm:ss}"));
        }
        else
        {
            Console.WriteLine(L($"  Expires:   unlimited", $"  Platnost:  neomezená"));
        }
        Console.WriteLine();

        // Copy URL to clipboard
        CopyToClipboard(url);
    }

    static int Error(string message)
    {
        Console.Error.WriteLine(L($"Error: {message}", $"Chyba: {message}"));
        PrintUsage();
        return 1;
    }

    /// <summary>
    /// Import configuration from a client config JSON file.
    /// Merges imported fields into existing config, preserving SMB-specific settings.
    /// Compatible with setup.php export and Android app export.
    /// </summary>
    static int ImportConfig(string importPath)
    {
        if (!File.Exists(importPath))
        {
            Console.Error.WriteLine(L($"Error: File not found: {importPath}",
                                      $"Chyba: Soubor nenalezen: {importPath}"));
            return 1;
        }

        try
        {
            var json = File.ReadAllText(importPath);
            var imported = JsonSerializer.Deserialize<JsonElement>(json);

            // Load existing config to preserve SMB-specific settings
            LoadConfig();

            // Apply imported values (only if present and non-empty)
            if (imported.TryGetProperty("UploadEndpoint", out var ep) && ep.GetString() is string endpoint && !string.IsNullOrWhiteSpace(endpoint))
            {
                Config.UploadEndpoint = endpoint;
            }

            if (imported.TryGetProperty("ApiKey", out var ak) && ak.GetString() is string apiKey && !string.IsNullOrWhiteSpace(apiKey))
            {
                Config.ApiKey = apiKey;
            }

            if (imported.TryGetProperty("ShareCodeLength", out var scl) && scl.TryGetInt32(out int codeLen) && codeLen >= 4 && codeLen <= 32)
            {
                Config.ShareCodeLength = codeLen;
            }

            // Derive BaseUrl from UploadEndpoint if not already set
            if (string.IsNullOrWhiteSpace(Config.BaseUrl) && !string.IsNullOrWhiteSpace(Config.UploadEndpoint))
            {
                var uri = new Uri(Config.UploadEndpoint);
                Config.BaseUrl = $"{uri.Scheme}://{uri.Host}";
            }

            // Save merged config
            var options = new JsonSerializerOptions { WriteIndented = true };
            var outputJson = JsonSerializer.Serialize(Config, options);
            File.WriteAllText(ConfigFilePath, outputJson);

            Console.WriteLine();
            Console.WriteLine(L($"✓ Configuration imported from: {importPath}",
                                $"✓ Konfigurace importována z: {importPath}"));
            Console.WriteLine(L($"  Saved to: {ConfigFilePath}",
                                $"  Uloženo do: {ConfigFilePath}"));
            Console.WriteLine();
            Console.WriteLine($"  Endpoint: {Config.UploadEndpoint}");
            Console.WriteLine(L($"  Code:     {Config.ShareCodeLength} chars",
                                $"  Kód:      {Config.ShareCodeLength} znaků"));
            if (IsSmbConfigured)
            {
                Console.WriteLine(L($"  SMB:      {Config.DataRoot} (preserved)",
                                    $"  SMB:      {Config.DataRoot} (zachováno)"));
            }
            Console.WriteLine();
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(L($"Import error: {ex.Message}",
                                      $"Chyba při importu: {ex.Message}"));
            return 1;
        }
    }

    static void PrintUsage()
    {
        Console.WriteLine();
        Console.WriteLine("EasyShare - " + L("File Sharing", "Sdílení souborů"));
        Console.WriteLine();
        Console.WriteLine(L("Usage:", "Použití:"));
        Console.WriteLine(L("  EasyShare.exe <mode> <path> [expiration]",
                            "  EasyShare.exe <režim> <cesta> [platnost]"));
        Console.WriteLine("  EasyShare.exe clipboard [" + L("expiration", "platnost") + "]");
        Console.WriteLine("  EasyShare.exe setup");
        Console.WriteLine();
        Console.WriteLine(L("Modes:", "Režimy:"));
        Console.WriteLine(L("  file      <file-path>          Share a single file",
                            "  file      <cesta-k-souboru>     Sdílet jeden soubor"));
        Console.WriteLine(L("  dir       <dir-path>           Share a directory (browsable)",
                            "  dir       <cesta-k-adresáři>    Sdílet adresář (procházení)"));
        Console.WriteLine(L("  zip       <dir-path>           ZIP a directory and share",
                            "  zip       <cesta-k-adresáři>    Zabalit adresář do ZIP a sdílet"));
        Console.WriteLine(L("  clipboard                      Share clipboard content (image/text)",
                            "  clipboard                       Sdílet obsah schránky (obrázek/text)"));
        Console.WriteLine(L("  setup                          Run configuration wizard",
                            "  setup                           Spustit průvodce konfigurací"));
        Console.WriteLine(L("  import    <json-path>          Import configuration from file",
                            "  import    <cesta-k-json>        Importovat konfiguraci ze souboru"));
        Console.WriteLine();
        Console.WriteLine(L("Transport: SMB (local network) -> HTTP fallback (upload endpoint)",
                            "Přenos: SMB (lokální síť) -> HTTP fallback (upload endpoint)"));
        Console.WriteLine();
        Console.WriteLine(L("Expiration (optional):", "Platnost (volitelná):"));
        Console.WriteLine(L("  Xd           X days (rounded to end of day)",
                            "  Xd           X dní (zaokrouhleno do konce dne)"));
        Console.WriteLine(L("  Xh           X hours (rounded to end of hour)",
                            "  Xh           X hodin (zaokrouhleno do konce hodiny)"));
        Console.WriteLine(L("  Xm           X minutes (rounded to end of minute)",
                            "  Xm           X minut (zaokrouhleno do konce minuty)"));
        Console.WriteLine(L("  yyyy-mm-dd   Specific date (valid until end of day)",
                            "  yyyy-mm-dd   Konkrétní datum (platí do konce dne)"));
        Console.WriteLine();
        Console.WriteLine(L("Examples:", "Příklady:"));
        Console.WriteLine("  EasyShare.exe file C:\\Documents\\report.pdf");
        Console.WriteLine("  EasyShare.exe file C:\\Documents\\report.pdf 7d");
        Console.WriteLine("  EasyShare.exe dir  C:\\Photos\\Vacation 2026-12-31");
        Console.WriteLine("  EasyShare.exe zip  C:\\Projects\\MyApp 24h");
        Console.WriteLine("  EasyShare.exe clipboard");
        Console.WriteLine("  EasyShare.exe clipboard 1d");
        Console.WriteLine("  EasyShare.exe import config.json");
        Console.WriteLine();

        if (File.Exists(ConfigFilePath))
        {
            Console.WriteLine(L($"Configuration: {ConfigFilePath}",
                                $"Konfigurace: {ConfigFilePath}"));
            if (IsSmbConfigured)
            {
                Console.WriteLine($"  SMB:  {Config.DataRoot}");
            }
            if (IsHttpConfigured)
            {
                Console.WriteLine($"  HTTP: {Config.UploadEndpoint}");
            }
            if (!string.IsNullOrWhiteSpace(Config.BaseUrl))
            {
                Console.WriteLine($"  URL:  {Config.BaseUrl}");
            }
            Console.WriteLine(L($"  Code: {Config.ShareCodeLength} chars",
                                $"  Kód:  {Config.ShareCodeLength} znaků"));
        }
        else
        {
            Console.WriteLine(L($"Configuration: NOT FOUND ({ConfigFilePath})",
                                $"Konfigurace: NENALEZENA ({ConfigFilePath})"));
            Console.WriteLine(L("Run 'EasyShare.exe setup' to create one.",
                                "Spusťte 'EasyShare.exe setup' pro vytvoření."));
        }
        Console.WriteLine();
    }
}
