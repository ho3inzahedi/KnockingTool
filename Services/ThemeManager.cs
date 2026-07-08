using System.Windows;
using System.Windows.Media;

namespace KnockingTool.Services;

public static class ThemeManager
{
    private static readonly Uri LightThemeUri = new("/Themes/LightTheme.xaml", UriKind.Relative);
    private static readonly Uri DarkThemeUri = new("/Themes/DarkTheme.xaml", UriKind.Relative);
    private static readonly Uri ControlsUri = new("/Themes/Controls.xaml", UriKind.Relative);

    public static bool IsDarkTheme { get; private set; }

    public static void Initialize(bool isDarkTheme)
    {
        var appResources = Application.Current.Resources;
        appResources.MergedDictionaries.Clear();
        appResources.MergedDictionaries.Add(new ResourceDictionary { Source = ControlsUri });
        appResources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = isDarkTheme ? DarkThemeUri : LightThemeUri
        });
        IsDarkTheme = isDarkTheme;
    }

    public static void SetTheme(bool isDarkTheme)
    {
        if (IsDarkTheme == isDarkTheme)
        {
            return;
        }

        var appResources = Application.Current.Resources;
        var themeDictionary = appResources.MergedDictionaries
            .FirstOrDefault(d => d.Source?.OriginalString is "Themes/LightTheme.xaml" or "Themes/DarkTheme.xaml"
                                 or "/Themes/LightTheme.xaml" or "/Themes/DarkTheme.xaml");

        if (themeDictionary is not null)
        {
            appResources.MergedDictionaries.Remove(themeDictionary);
        }

        appResources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = isDarkTheme ? DarkThemeUri : LightThemeUri
        });

        IsDarkTheme = isDarkTheme;
    }

    public static SolidColorBrush GetBrush(string key)
        => Application.Current.FindResource(key) as SolidColorBrush
           ?? Brushes.Transparent;
}
