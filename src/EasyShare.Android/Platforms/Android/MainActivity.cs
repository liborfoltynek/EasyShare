using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace EasyShare.Android;

[Activity(
    Theme = "@style/Maui.MainTheme.NoActionBar",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTask,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
[IntentFilter(
    new[] { Intent.ActionSend },
    Categories = new[] { Intent.CategoryDefault },
    DataMimeType = "*/*",
    Label = "EasyShare")]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        HandleIntent(Intent);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        if (intent != null)
        {
            HandleIntent(intent);
        }
    }

    private void HandleIntent(Intent? intent)
    {
        if (intent?.Action != Intent.ActionSend)
        {
            return;
        }

        var uri = intent.GetParcelableExtra(Intent.ExtraStream) as global::Android.Net.Uri;
        if (uri == null)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                string fileName = GetFileName(uri) ?? "shared_file";

                var tempDir = Path.Combine(FileSystem.CacheDirectory, "shared");
                Directory.CreateDirectory(tempDir);
                var tempPath = Path.Combine(tempDir, fileName);

                using (var inputStream = ContentResolver?.OpenInputStream(uri))
                {
                    if (inputStream == null) {
                        return;
                    }

                    using var fileStream = File.Create(tempPath);
                    await inputStream.CopyToAsync(fileStream);
                }

                await WaitForMainPageAndHandleFile(tempPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling shared file: {ex}");
            }
        });
    }

    private async Task WaitForMainPageAndHandleFile(string filePath)
    {
        for (int i = 0; i < 50; i++)
        {
            await Task.Delay(100);
            var shell = Shell.Current;
            if (shell?.CurrentPage is MainPage mainPage)
            {
                await mainPage.HandleSharedFilePathAsync(filePath);
                return;
            }
        }
    }

    private string? GetFileName(global::Android.Net.Uri uri)
    {
        string? fileName = null;

        if (uri.Scheme == "content")
        {
            using var cursor = ContentResolver?.Query(uri, null, null, null, null);
            if (cursor != null && cursor.MoveToFirst())
            {
                int nameIndex = cursor.GetColumnIndex(global::Android.Provider.OpenableColumns.DisplayName);
                if (nameIndex >= 0)
                {
                    fileName = cursor.GetString(nameIndex);
                }
            }
        }

        if (string.IsNullOrEmpty(fileName))
        {
            fileName = Path.GetFileName(uri.Path);
        }

        return fileName;
    }
}
