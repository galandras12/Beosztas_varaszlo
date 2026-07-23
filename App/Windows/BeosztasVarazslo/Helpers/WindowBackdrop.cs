using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace BeosztasVarazslo.Helpers;

/// <summary>
/// A címsort a rendszer sötét/világos témájához igazítja, és Windows 11 (22H2+) rendszeren
/// a Mica anyagtípust is beállítja az ablakon a DWM API-n keresztül. Mivel az ablak tartalmi
/// része (a kártyák, navigációs sáv) szándékosan átlátszatlan hátteret használ (biztonsági
/// megfontolásból, hogy régebbi Windows-on se torzuljon a megjelenés), a Mica anyag ténylegesen
/// legfeljebb a natív címsor sávján válhat láthatóvá - ez nem hiba, hanem tudatos, kockázatmentes
/// megoldás. Régebbi Windows verziókon a hívás egyszerűen hatástalan (nem dob hibát).
/// </summary>
public static class WindowBackdrop
{
    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int valueSize);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
    private const int DWMSBT_MAINWINDOW = 2; // Mica

    public static void Apply(Window window, bool isDarkTheme)
    {
        window.SourceInitialized += (_, _) =>
        {
            try
            {
                var hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd == IntPtr.Zero) return;

                int darkMode = isDarkTheme ? 1 : 0;
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

                int backdrop = DWMSBT_MAINWINDOW;
                DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int));
            }
            catch
            {
                // Régebbi Windows verzión vagy egyéb korlátozás esetén egyszerűen a
                // sima (Mica nélküli) ablakháttér marad érvényben.
            }
        };
    }
}
