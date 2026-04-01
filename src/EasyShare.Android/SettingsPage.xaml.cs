using System.Text.Json;

namespace EasyShare.Android;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();

        // Set localized strings
        Title = Strings.SettingsTitle;
        SettingsTitleLabel.Text = Strings.Settings;
        ApiKeyLabel.Text = Strings.ApiKeyLabel;
        CodeLengthLabel.Text = Strings.CodeLengthLabel;
        CodeLengthHintLabel.Text = Strings.CodeLengthHint;
        SaveButton.Text = Strings.Save;
        BackupTitleLabel.Text = Strings.BackupRestore;
        BackupDescLabel.Text = Strings.BackupRestoreDesc;
        ImportButton.Text = Strings.Import;
        ExportButton.Text = Strings.Export;
        BackButton.Text = Strings.Back;

        LoadSettings();
    }

    private void LoadSettings()
    {
        EndpointEntry.Text = Preferences.Get("upload_endpoint", "");
        ApiKeyEntry.Text = Preferences.Get("api_key", "");
        CodeLengthEntry.Text = Preferences.Get("share_code_length", 8).ToString();
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        var endpoint = EndpointEntry.Text?.Trim() ?? "";
        var apiKey = ApiKeyEntry.Text?.Trim() ?? "";
        var codeLengthStr = CodeLengthEntry.Text?.Trim() ?? "8";

        if (string.IsNullOrEmpty(endpoint))
        {
            ShowMessage(Strings.FillEndpoint, true);
            return;
        }

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out _))
        {
            ShowMessage(Strings.InvalidUrl, true);
            return;
        }

        if (string.IsNullOrEmpty(apiKey))
        {
            ShowMessage(Strings.FillApiKey, true);
            return;
        }

        if (!int.TryParse(codeLengthStr, out int codeLength) || codeLength < 4 || codeLength > 32)
        {
            ShowMessage(Strings.InvalidCodeLength, true);
            return;
        }

        Preferences.Set("upload_endpoint", endpoint);
        Preferences.Set("api_key", apiKey);
        Preferences.Set("share_code_length", codeLength);
        Preferences.Set("is_configured", true);

        ShowMessage(Strings.SettingsSaved, false);
    }

    // ════════════════════════════════════════════════════════════════
    //  Import — read client config JSON (from setup.php or other device)
    // ════════════════════════════════════════════════════════════════

    private async void OnImportClicked(object? sender, EventArgs e)
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = Strings.ImportPickTitle,
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.Android, new[] { "application/json", "text/plain", "*/*" } },
                    { DevicePlatform.iOS, new[] { "public.json", "public.plain-text" } },
                    { DevicePlatform.WinUI, new[] { ".json" } },
                })
            });

            if (result == null) {
                return; // user cancelled
            }

            using var stream = await result.OpenReadAsync();
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();

            var doc = JsonSerializer.Deserialize<JsonElement>(json);

            int imported = 0;

            if (doc.TryGetProperty("UploadEndpoint", out var ep) && ep.GetString() is string endpoint && !string.IsNullOrWhiteSpace(endpoint))
            {
                Preferences.Set("upload_endpoint", endpoint);
                imported++;
            }

            if (doc.TryGetProperty("ApiKey", out var ak) && ak.GetString() is string apiKey && !string.IsNullOrWhiteSpace(apiKey))
            {
                Preferences.Set("api_key", apiKey);
                imported++;
            }

            if (doc.TryGetProperty("ShareCodeLength", out var scl) && scl.TryGetInt32(out int codeLen) && codeLen >= 4 && codeLen <= 32)
            {
                Preferences.Set("share_code_length", codeLen);
                imported++;
            }

            if (imported == 0)
            {
                ShowMessage(Strings.InvalidConfigFile, true);
                return;
            }

            Preferences.Set("is_configured", true);
            LoadSettings(); // refresh UI
            ShowMessage(Strings.Imported(imported), false);
        }
        catch (Exception ex)
        {
            ShowMessage(Strings.ImportError(ex.Message), true);
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  Export — save current config as JSON (same format as setup.php)
    // ════════════════════════════════════════════════════════════════

    private async void OnExportClicked(object? sender, EventArgs e)
    {
        try
        {
            var endpoint = Preferences.Get("upload_endpoint", "");
            var apiKey = Preferences.Get("api_key", "");
            var codeLength = Preferences.Get("share_code_length", 8);

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
            {
                ShowMessage(Strings.FillConfigFirst, true);
                return;
            }

            var config = new Dictionary<string, object>
            {
                ["UploadEndpoint"] = endpoint,
                ["ApiKey"] = apiKey,
                ["ShareCodeLength"] = codeLength,
            };

            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });

            // Determine filename from endpoint host
            string host = "easyshare";
            if (Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
            {
                host = uri.Host;
            }
            string filename = $"{host}_client.json";

            // Write to cache dir and share
            var cacheDir = FileSystem.CacheDirectory;
            var filePath = Path.Combine(cacheDir, filename);
            File.WriteAllText(filePath, json);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = Strings.ExportTitle,
                File = new ShareFile(filePath)
            });
        }
        catch (Exception ex)
        {
            ShowMessage(Strings.ExportError(ex.Message), true);
        }
    }

    // ════════════════════════════════════════════════════════════════

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private void ShowMessage(string text, bool isError)
    {
        MessageLabel.Text = text;
        MessageLabel.TextColor = isError
            ? (Color)Application.Current!.Resources["ErrorRed"]
            : (Color)Application.Current!.Resources["SuccessGreen"];
        MessageLabel.IsVisible = true;
    }
}
