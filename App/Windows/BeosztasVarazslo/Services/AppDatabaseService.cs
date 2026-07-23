using System.IO;
using System.Text.Json;
using BeosztasVarazslo.Models;

namespace BeosztasVarazslo.Services;

/// <summary>
/// Betöltés/mentés egyetlen titkosítatlan JSON fájlba - ez a program belső,
/// szerver nélküli adatbázisa (alapértelmezetten a felhasználó AppData mappájában).
/// </summary>
public static class AppDatabaseService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static readonly IReadOnlyList<string> MonthNames = new[]
    {
        "Január", "Február", "Március", "Április", "Május", "Június",
        "Július", "Augusztus", "Szeptember", "Október", "November", "December"
    };

    public static readonly IReadOnlyDictionary<int, (double Hours, int Days)> DefaultMonthHours =
        new Dictionary<int, (double Hours, int Days)>
        {
            [1] = (176, 22), [2] = (168, 21), [3] = (152, 19), [4] = (168, 21),
            [5] = (168, 21), [6] = (160, 20), [7] = (184, 23), [8] = (168, 21),
            [9] = (168, 21), [10] = (176, 22), [11] = (160, 20), [12] = (160, 20)
        };

    public static string GetDefaultDbPath()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BeosztasVarazslo");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "adatbazis.json");
    }

    public static AppDatabase Load(string? path = null)
    {
        path ??= GetDefaultDbPath();
        AppDatabase db;
        try
        {
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                db = JsonSerializer.Deserialize<AppDatabase>(json, JsonOptions) ?? CreateDefault();
            }
            else
            {
                db = CreateDefault();
            }
        }
        catch
        {
            db = CreateDefault();
        }
        EnsureSeeded(db);
        return db;
    }

    public static void Save(AppDatabase db, string? path = null)
    {
        path ??= GetDefaultDbPath();
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(db, JsonOptions);
        var tmpPath = path + ".tmp";
        File.WriteAllText(tmpPath, json);
        File.Copy(tmpPath, path, overwrite: true);
        File.Delete(tmpPath);
    }

    /// <summary>Fájlból beolvas egy adatbázist (pl. korábbi export) anélkül, hogy elmentené.</summary>
    public static AppDatabase ImportFromFile(string path)
    {
        var json = File.ReadAllText(path);
        var db = JsonSerializer.Deserialize<AppDatabase>(json, JsonOptions)
                 ?? throw new InvalidDataException("A fájl formátuma nem megfelelő.");
        EnsureSeeded(db);
        return db;
    }

    public static void ExportToFile(AppDatabase db, string path) => Save(db, path);

    public static AppDatabase CreateDefault()
    {
        var db = new AppDatabase();
        ResetMonthHours(db);
        SeedDefaultGroups(db);
        return db;
    }

    public static void EnsureSeeded(AppDatabase db)
    {
        if (db.MonthHours.Count == 0) ResetMonthHours(db);
        if (db.Groups.Count == 0) SeedDefaultGroups(db);
    }

    public static void ResetMonthHours(AppDatabase db)
    {
        db.MonthHours.Clear();
        foreach (var kv in DefaultMonthHours)
            db.MonthHours[kv.Key] = new MonthHours { Hours = kv.Value.Hours, Days = kv.Value.Days };
    }

    private static long _idCounter = DateTime.UtcNow.Ticks;
    public static long NextId() => Interlocked.Increment(ref _idCounter);

    private static void SeedDefaultGroups(AppDatabase db)
    {
        var apolok = new WorkGroup { Id = NextId(), Name = "Ápolók", Type = GroupTypes.General, DailyHours = 12, StaffPerShift = 2 };
        apolok.ShiftTypes.Add(new ShiftType { Code = "N", Label = "Nappal", Hours = 12 });
        apolok.ShiftTypes.Add(new ShiftType { Code = "É", Label = "Éjszaka", Hours = 12 });
        db.Groups.Add(apolok);

        var takarito = new WorkGroup { Id = NextId(), Name = "Takarítók", Type = GroupTypes.General, DailyHours = 8, StaffPerShift = 0 };
        takarito.ShiftTypes.Add(new ShiftType { Code = "M", Label = "Munka", Hours = 8 });
        db.Groups.Add(takarito);

        var reszmunka = new WorkGroup { Id = NextId(), Name = "Részmunkaidősök", Type = GroupTypes.General, DailyHours = 4, StaffPerShift = 0 };
        reszmunka.ShiftTypes.Add(new ShiftType { Code = "M", Label = "Munka", Hours = 4 });
        db.Groups.Add(reszmunka);

        var iroda = new WorkGroup { Id = NextId(), Name = "Irodai dolgozók", Type = GroupTypes.Office, DailyHours = 8, StaffPerShift = 0 };
        iroda.ShiftTypes.Add(new ShiftType { Code = "M", Label = "Munka", Hours = 8 });
        db.Groups.Add(iroda);
    }
}
