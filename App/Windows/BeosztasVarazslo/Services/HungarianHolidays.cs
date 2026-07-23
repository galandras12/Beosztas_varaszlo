namespace BeosztasVarazslo.Services;

/// <summary>Magyar munkaszüneti napok (fix + húsvéthez kötött mozgó ünnepek) számítása.</summary>
public static class HungarianHolidays
{
    private static string Iso(int year, int month, int day) => $"{year:D4}-{month:D2}-{day:D2}";

    /// <summary>Gauss-algoritmus a húsvétvasárnap kiszámítására (Gergely-naptár).</summary>
    private static DateTime EasterSunday(int year)
    {
        int a = year % 19;
        int b = year / 100;
        int c = year % 100;
        int d = b / 4;
        int e = b % 4;
        int f = (b + 8) / 25;
        int g = (b - f + 1) / 3;
        int h = (19 * a + b - d - g + 15) % 30;
        int i = c / 4;
        int k = c % 4;
        int l = (32 + 2 * e + 2 * i - h - k) % 7;
        int m = (a + 11 * h + 22 * l) / 451;
        int month = (h + l - 7 * m + 114) / 31;
        int day = ((h + l - 7 * m + 114) % 31) + 1;
        return new DateTime(year, month, day);
    }

    /// <summary>Egy adott év automatikusan generált munkaszüneti napjai: ISO dátum -> ünnep neve.</summary>
    public static Dictionary<string, string> DefaultHolidays(int year)
    {
        var easter = EasterSunday(year);
        var goodFriday = easter.AddDays(-2);
        var easterMonday = easter.AddDays(1);
        var whitMonday = easter.AddDays(50);

        var map = new Dictionary<string, string>
        {
            [Iso(year, 1, 1)] = "Újév",
            [Iso(year, 3, 15)] = "Nemzeti ünnep (1848)",
            [Iso(goodFriday.Year, goodFriday.Month, goodFriday.Day)] = "Nagypéntek",
            [Iso(easterMonday.Year, easterMonday.Month, easterMonday.Day)] = "Húsvéthétfő",
            [Iso(year, 5, 1)] = "A munka ünnepe",
            [Iso(whitMonday.Year, whitMonday.Month, whitMonday.Day)] = "Pünkösdhétfő",
            [Iso(year, 8, 20)] = "Az államalapítás ünnepe",
            [Iso(year, 10, 23)] = "Az 1956-os forradalom ünnepe",
            [Iso(year, 11, 1)] = "Mindenszentek",
            [Iso(year, 12, 25)] = "Karácsony",
            [Iso(year, 12, 26)] = "Karácsony másnapja"
        };
        return map;
    }
}
