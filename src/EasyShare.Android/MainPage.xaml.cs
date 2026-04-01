using EasyShare.Android.Services;

namespace EasyShare.Android;

public partial class MainPage : ContentPage
{
    private readonly FileUploadService _uploadService;
    private string? _selectedFilePath;
    private Stream? _sharedStream;
    private string? _sharedFileName;
    private CancellationTokenSource? _uploadCts;

    public MainPage()
    {
        InitializeComponent();
        _uploadService = new FileUploadService();

        // Set localized strings
        SelectedFileLabel.Text = "📎 " + Strings.SelectedFile;
        ExpiryLabel.Text = Strings.LinkExpiry;
        UploadingLabel.Text = Strings.Uploading;
        UploadedLabel.Text = Strings.Uploaded;
        ShareLinkLabel.Text = Strings.ShareLink;
        ClipboardConfirmLabel.Text = Strings.CopiedToClipboard;
        CopyButton.Text = Strings.Copy;
        ShareButton.Text = Strings.ShareUrl;
        PickFileButton.Text = Strings.PickFile;
        UploadButton.Text = Strings.UploadAndShare;
        ResetButton.Text = Strings.NewUpload;
        ErrorTitleLabel.Text = Strings.Error;

        // Populate expiry picker with localized options
        ExpiryPicker.ItemsSource = new[]
        {
            Strings.NoLimit, Strings.OneHour, Strings.TwentyFourHours,
            Strings.SevenDays, Strings.ThirtyDays
        };
        ExpiryPicker.SelectedIndex = 3; // Default: 7 days

        UpdateServerLabel();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateServerLabel();
    }

    private void UpdateServerLabel()
    {
        var endpoint = _uploadService.UploadEndpoint;
        if (!string.IsNullOrEmpty(endpoint) && Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
        {
            ServerLabel.Text = uri.Host;
        }
        else
        {
            ServerLabel.Text = Strings.ServerNotSet;
        }
    }

    private async void OnSettingsClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("Settings");
    }

    public async Task HandleSharedFileAsync(Stream stream, string fileName)
    {
        _sharedStream = stream;
        _sharedFileName = fileName;
        _selectedFilePath = null;

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            ShowFileInfo(fileName, stream.CanSeek ? stream.Length : -1);
        });

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await PerformUploadAsync();
        });
    }

    public async Task HandleSharedFilePathAsync(string filePath)
    {
        _selectedFilePath = filePath;
        _sharedStream = null;
        _sharedFileName = null;

        var fileInfo = new FileInfo(filePath);

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            ShowFileInfo(fileInfo.Name, fileInfo.Length);
        });

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await PerformUploadAsync();
        });
    }

    private void OnPickFileClicked(object? sender, EventArgs e)
    {
        _ = PickFileAsync();
    }

    private async Task PickFileAsync()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = Strings.PickFileTitle
            });

            if (result == null) {
                return;
            }

            _selectedFilePath = result.FullPath;
            _sharedStream = null;
            _sharedFileName = null;

            var fileInfo = new FileInfo(result.FullPath);
            ShowFileInfo(result.FileName, fileInfo.Length);
        }
        catch (Exception ex)
        {
            ShowError(Strings.CannotPickFile(ex.Message));
        }
    }

    private void ShowFileInfo(string fileName, long sizeBytes)
    {
        FileNameLabel.Text = fileName;
        FileSizeLabel.Text = sizeBytes >= 0 ? FormatSize(sizeBytes) : Strings.SizeUnknown;
        FileInfoFrame.IsVisible = true;
        UploadButton.IsEnabled = true;
        UploadButton.Opacity = 1.0;
        ResultFrame.IsVisible = false;
        ErrorFrame.IsVisible = false;
        ResetButton.IsVisible = false;
    }

    private void OnUploadClicked(object? sender, EventArgs e)
    {
        _ = PerformUploadAsync();
    }

    private async Task PerformUploadAsync()
    {
        if (!_uploadService.IsConfigured)
        {
            ShowError(Strings.ServerNotConfigured);
            return;
        }

        string? expires = ExpiryPicker.SelectedIndex switch
        {
            1 => "1h",
            2 => "24h",
            3 => "7d",
            4 => "30d",
            _ => null
        };

        PickFileButton.IsEnabled = false;
        UploadButton.IsEnabled = false;
        UploadButton.Opacity = 0.5;
        ProgressFrame.IsVisible = true;
        UploadProgressBar.Progress = 0;
        ProgressLabel.Text = "0 %";
        ResultFrame.IsVisible = false;
        ErrorFrame.IsVisible = false;

        var progress = new Progress<double>(p =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                UploadProgressBar.Progress = p;
                ProgressLabel.Text = $"{(int)(p * 100)} %";
            });
        });

        _uploadCts = new CancellationTokenSource();

        try
        {
            UploadResult result;

            if (_sharedStream != null && _sharedFileName != null)
            {
                result = await _uploadService.UploadStreamAsync(
                    _sharedStream, _sharedFileName, expires, progress, _uploadCts.Token);
            }
            else if (_selectedFilePath != null)
            {
                result = await _uploadService.UploadFileAsync(
                    _selectedFilePath, expires, progress, _uploadCts.Token);
            }
            else
            {
                ShowError(Strings.NoFileSelected);
                return;
            }

            ProgressFrame.IsVisible = false;

            if (result.Success && result.Url != null)
            {
                ResultUrlLabel.Text = result.Url;
                ResultFrame.IsVisible = true;

                if (result.Expires != null)
                {
                    if (DateTimeOffset.TryParse(result.Expires, out var expiresDto))
                    {
                        var local = expiresDto.ToLocalTime();
                        ExpiryInfoLabel.Text = Strings.ValidUntil($"{local:d.M.yyyy H:mm}");
                    }
                    else
                    {
                        ExpiryInfoLabel.Text = Strings.ValidUntil(result.Expires);
                    }
                    ExpiryInfoLabel.IsVisible = true;
                }
                else
                {
                    ExpiryInfoLabel.IsVisible = false;
                }

                await Clipboard.Default.SetTextAsync(result.Url);
                ClipboardConfirmLabel.IsVisible = true;

                ResetButton.IsVisible = true;
                FileInfoFrame.IsVisible = false;
            }
            else
            {
                ShowError(result.Error ?? Strings.UnknownError);
            }
        }
        catch (TaskCanceledException)
        {
            ProgressFrame.IsVisible = false;
            ShowError(Strings.UploadCancelled);
        }
        catch (Exception ex)
        {
            ProgressFrame.IsVisible = false;
            ShowError(Strings.UploadError(ex.Message));
        }
        finally
        {
            PickFileButton.IsEnabled = true;
            UploadButton.IsEnabled = _selectedFilePath != null || _sharedStream != null;
            UploadButton.Opacity = UploadButton.IsEnabled ? 1.0 : 0.5;
            _uploadCts = null;
        }
    }

    private async void OnCopyUrlClicked(object? sender, EventArgs e)
    {
        var url = ResultUrlLabel.Text;
        if (!string.IsNullOrEmpty(url))
        {
            await Clipboard.Default.SetTextAsync(url);
            ClipboardConfirmLabel.Text = Strings.CopiedToClipboard;
            ClipboardConfirmLabel.IsVisible = true;
        }
    }

    private async void OnShareUrlClicked(object? sender, EventArgs e)
    {
        var url = ResultUrlLabel.Text;
        if (!string.IsNullOrEmpty(url))
        {
            await Share.Default.RequestAsync(new ShareTextRequest
            {
                Uri = url,
                Title = Strings.ShareTitle
            });
        }
    }

    private async void OnUrlTapped(object? sender, TappedEventArgs e)
    {
        var url = ResultUrlLabel.Text;
        if (!string.IsNullOrEmpty(url))
        {
            await Clipboard.Default.SetTextAsync(url);
            ClipboardConfirmLabel.Text = Strings.CopiedToClipboard;
            ClipboardConfirmLabel.IsVisible = true;
        }
    }

    private void OnResetClicked(object? sender, EventArgs e)
    {
        _selectedFilePath = null;
        _sharedStream?.Dispose();
        _sharedStream = null;
        _sharedFileName = null;

        FileInfoFrame.IsVisible = false;
        ProgressFrame.IsVisible = false;
        ResultFrame.IsVisible = false;
        ErrorFrame.IsVisible = false;
        ResetButton.IsVisible = false;
        UploadButton.IsEnabled = false;
        UploadButton.Opacity = 0.5;
        ClipboardConfirmLabel.IsVisible = false;
    }

    private void ShowError(string message)
    {
        ProgressFrame.IsVisible = false;
        ErrorLabel.Text = message;
        ErrorFrame.IsVisible = true;
        PickFileButton.IsEnabled = true;
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1048576) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1073741824) return $"{bytes / 1048576.0:F1} MB";
        return $"{bytes / 1073741824.0:F2} GB";
    }
}
