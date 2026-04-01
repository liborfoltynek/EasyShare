namespace EasyShare.Android;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("Settings", typeof(SettingsPage));
    }
}
