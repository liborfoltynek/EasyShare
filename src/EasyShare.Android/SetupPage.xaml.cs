using System.Text.Json;

namespace EasyShare.Android;

public partial class SetupPage : ContentPage
{
    public SetupPage()
    {
        InitializeComponent();

        // Set localized strings
        SetupTitleLabel.Text = Strings.SetupServerTitle;
        SetupDescLabel.Text = Strings.SetupDesc;
        ImportFileButton.Text = Strings.ImportFromFile;
        PasteJsonHintLabel.Text = Strings.PasteJsonHint;
        JsonPasteEditor.Placeholder = Strings.PasteJsonPlaceholder;
        ApplyJsonButton.Text = Strings.ApplyPastedJson;
        OrManualLabel.Text = Strings.SetupOrImport;
        EndpointHelpLabel.Text = Strings.EndpointHelp;
        ApiKeyLabel.Text = Strings.ApiKeyLabel;
        ApiKeyHelpLabel.Text = Strings.ApiKeyHelp;
        CodeLengthLabel.Text = Strings.CodeLengthLabel;
        CodeLengthHelpLabel.Text = Strings.CodeLengthHelp;
        SaveButton.Text = Strings.SaveAndContinue;

        // Pre-fill with existing values if any
        var endpoint = Preferences.Get("upload_endpoint", "");
        var apiKey = Preferences.Get("api_key", "");
        var codeLength = Preferences.Get("share_code_length", 8);

        if (!string.IsNullOrEmpty(endpoint)) {
            EndpointEntry.Text = endpoint;
        }
        if (!string.IsNullOrEmpty(apiKey)) {
            ApiKeyEntry.Text = apiKey;
        }
        CodeLengthEntry.Text = codeLength.ToString();
    }

    // ════════════════════════════════════════════════════════════════
    //  Import from file
    // ════════════════════════════════════════════════════════════════

    private async void OnImportFileClicked(object? sender, EventArgs e)
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

            ApplyJsonConfig(json);
        }
        catch (Exception ex)
        {
            ShowError(Strings.ImportError(ex.Message));
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  Apply pasted JSON
    // ════════════════════════════════════════════════════════════════

    private void OnApplyJsonClicked(object? sender, EventArgs e)
    {
        var json = JsonPasteEditor.Text?.Trim();

        if (string.IsNullOrWhiteSpace(json))
        {
            ShowError(Strings.PasteEmpty);
            return;
        }

        ApplyJsonConfig(json);
    }

    // ════════════════════════════════════════════════════════════════
    //  Shared JSON parser — applies config from JSON string
    // ════════════════════════════════════════════════════════════════

    private void ApplyJsonConfig(string json)
    {
        try
        {
            var doc = JsonSerializer.Deserialize<JsonElement>(json);
            int imported = 0;

            string? endpoint = null;
            string? apiKey = null;
            int? codeLength = null;

            if (doc.TryGetProperty("UploadEndpoint", out var ep) && ep.GetString() is string epStr && !string.IsNullOrWhiteSpace(epStr))
            {
                endpoint = epStr;
                imported++;
            }

            if (doc.TryGetProperty("ApiKey", out var ak) && ak.GetString() is string akStr && !string.IsNullOrWhiteSpace(akStr))
            {
                apiKey = akStr;
                imported++;
            }

            if (doc.TryGetProperty("ShareCodeLength", out var scl) && scl.TryGetInt32(out int cl) && cl >= 4 && cl <= 32)
            {
                codeLength = cl;
                imported++;
            }

            if (imported == 0)
            {
                ShowError(Strings.InvalidConfigFile);
                return;
            }

            // Fill the form fields (user can review before saving)
            if (endpoint != null) {
                EndpointEntry.Text = endpoint;
            }
            if (apiKey != null) {
                ApiKeyEntry.Text = apiKey;
            }
            if (codeLength != null) {
                CodeLengthEntry.Text = codeLength.ToString();
            }

            ShowSuccess(Strings.Imported(imported));
        }
        catch (JsonException)
        {
            ShowError(Strings.InvalidJson);
        }
        catch (Exception ex)
        {
            ShowError(Strings.ImportError(ex.Message));
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  Manual save
    // ════════════════════════════════════════════════════════════════

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        var endpoint = EndpointEntry.Text?.Trim() ?? "";
        var apiKey = ApiKeyEntry.Text?.Trim() ?? "";
        var codeLengthStr = CodeLengthEntry.Text?.Trim() ?? "8";

        if (string.IsNullOrEmpty(endpoint))
        {
            ShowError(Strings.FillEndpoint);
            return;
        }

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out _))
        {
            ShowError(Strings.InvalidUrl);
            return;
        }

        if (string.IsNullOrEmpty(apiKey))
        {
            ShowError(Strings.FillApiKey);
            return;
        }

        if (!int.TryParse(codeLengthStr, out int codeLength) || codeLength < 4 || codeLength > 32)
        {
            ShowError(Strings.InvalidCodeLength);
            return;
        }

        // Save to preferences
        Preferences.Set("upload_endpoint", endpoint);
        Preferences.Set("api_key", apiKey);
        Preferences.Set("share_code_length", codeLength);
        Preferences.Set("is_configured", true);

        // Navigate to main page
        if (Shell.Current != null)
        {
            Application.Current!.MainPage = new AppShell();
        }
    }

    // ════════════════════════════════════════════════════════════════

    private void ShowError(string message)
    {
        SuccessLabel.IsVisible = false;
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    private void ShowSuccess(string message)
    {
        ErrorLabel.IsVisible = false;
        SuccessLabel.Text = message;
        SuccessLabel.IsVisible = true;
    }
}
