using Microsoft.Win32;

namespace BeosztasVarazslo.Helpers;

public static class ThemeDetector
{
    public static bool IsSystemDarkTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            if (value is int lightThemeFlag)
                return lightThemeFlag == 0;
        }
        catch
        {
            // Ha nem olvasható a beállítás, világos témát feltételezünk.
        }
        return false;
    }
}
