namespace EasyShare.Android;

public partial class SetupPage : ContentPage
{
    public SetupPage()
    {
        InitializeComponent();

        // Set localized strings
        SetupTitleLabel.Text = Strings.SetupServerTitle;
        SetupDescLabel.Text = Strings.SetupDesc;
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

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }
}
