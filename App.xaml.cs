using System.Windows;
using KnockingTool.Services;

namespace KnockingTool;

public partial class App : Application
{
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        var persistence = new PersistenceService();
        var (nodes, repeat, appSettings) = persistence.LoadAll();
        ThemeManager.Initialize(appSettings.IsDarkTheme);

        var mainWindow = new MainWindow(persistence, nodes, repeat, appSettings);
        mainWindow.Show();
    }
}
