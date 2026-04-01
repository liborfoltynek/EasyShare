namespace EasyShare.Android;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Show setup wizard if not configured
        bool isConfigured = Preferences.Get("is_configured", false);
        if (!isConfigured)
        {
            MainPage = new NavigationPage(new SetupPage());
        }
        else
        {
            MainPage = new AppShell();
        }
    }
}
