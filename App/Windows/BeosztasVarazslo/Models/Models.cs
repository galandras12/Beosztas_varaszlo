namespace BeosztasVarazslo.Models;

public static class GroupTypes
{
    public const string General = "ALTALANOS";
    public const string Office = "IRODA";
}

public static class ShiftCodes
{
    public const string Vacation = "SZ";
    public const string Sick = "BSZ";
    public const string Absence = "H";
    public const string Rest = "P";
}

public class ShiftType
{
    public string Code { get; set; } = "";
    public string Label { get; set; } = "";
    public double Hours { get; set; }
}

public class WorkGroup
{
    public long Id { get; set; }
    public string Name { get; set; } = "";

    /// <summary>GroupTypes.General (bármely nap, akár váltásos) vagy GroupTypes.Office (hétfő-péntek).</summary>
    public string Type { get; set; } = GroupTypes.General;
    public double DailyHours { get; set; }

    /// <summary>Egy műszakban szükséges létszám; 0 = nincs figyelve.</summary>
    public int StaffPerShift { get; set; }
    public List<ShiftType> ShiftTypes { get; set; } = new();
}

public class Employee
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public long GroupId { get; set; }

    /// <summary>1.0 = teljes munkaidő, 0.5 = fél (pl. napi 4 óra), stb.</summary>
    public double EmploymentFactor { get; set; } = 1.0;

    /// <summary>Kötelezően kitöltendő: évente kiadható szabadságnapok max. száma.</summary>
    public int MaxVacationDays { get; set; }
    public string Notes { get; set; } = "";
}

public class MonthHours
{
    public double Hours { get; set; }
    public int Days { get; set; }
}

/// <summary>A teljes alkalmazás-állapot - ez szerializálódik egyetlen titkosítatlan JSON fájlba.</summary>
public class AppDatabase
{
    public int Version { get; set; } = 1;

    /// <summary>Hónap (1-12) -> alap havi óraszám/munkanap.</summary>
    public Dictionary<int, MonthHours> MonthHours { get; set; } = new();

    public List<WorkGroup> Groups { get; set; } = new();
    public List<Employee> Employees { get; set; } = new();

    /// <summary>Év -> (ISO dátum -> egyéni ünnep neve).</summary>
    public Dictionary<int, Dictionary<string, string>> HolidaysExtra { get; set; } = new();

    /// <summary>Év -> kikapcsolt automatikus ünnepnapok (ISO dátum) listája.</summary>
    public Dictionary<int, List<string>> HolidaysRemoved { get; set; } = new();

    /// <summary>"year-month" kulcs -> dolgozóId -> (nap -> kód).</summary>
    public Dictionary<string, Dictionary<long, Dictionary<int, string>>> Schedule { get; set; } = new();

    /// <summary>"year-month" kulcs -> dolgozóId -> bejövő (kézzel beírt) óra.</summary>
    public Dictionary<string, Dictionary<long, double>> CarryOver { get; set; } = new();

    public static string MonthKey(int year, int month) => $"{year}-{month}";
}
