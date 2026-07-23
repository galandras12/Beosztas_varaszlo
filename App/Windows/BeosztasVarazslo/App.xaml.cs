using System.Windows;
using BeosztasVarazslo.Helpers;

namespace BeosztasVarazslo;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        var themeUri = new Uri(ThemeDetector.IsSystemDarkTheme() ? "Themes/Dark.xaml" : "Themes/Light.xaml", UriKind.Relative);
        var themeDict = new ResourceDictionary { Source = themeUri };
        Resources.MergedDictionaries.Insert(0, themeDict);

        base.OnStartup(e);
    }
}
