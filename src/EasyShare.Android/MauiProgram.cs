using Microsoft.Maui.Hosting;

namespace EasyShare.Android;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
            });

        builder.Services.AddSingleton<Services.FileUploadService>();

        return builder.Build();
    }
}
