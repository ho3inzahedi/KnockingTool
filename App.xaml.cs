using System.Windows;
using System.Windows.Threading;
using KnockingTool.Services;

namespace KnockingTool;

public partial class App : Application
{
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        try
        {
            var persistence = new PersistenceService();
            var (nodes, repeat, appSettings) = persistence.LoadAll();
            ThemeManager.Initialize(appSettings.IsDarkTheme);

            var mainWindow = new MainWindow(persistence, nodes, repeat, appSettings);
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Startup error:\n{ex}", "Knocking Tool", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show($"Unhandled error:\n{e.Exception}", "Knocking Tool", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
        Current.Shutdown(1);
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            MessageBox.Show($"Fatal error:\n{ex}", "Knocking Tool", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
