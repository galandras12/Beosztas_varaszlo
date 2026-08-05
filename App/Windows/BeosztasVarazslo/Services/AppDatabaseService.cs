using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
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

    // ---------- Közös, platformfüggetlen export/import (web/Android/Windows között is közös) ----------
    // A "Adatbázis mentése/betöltése" gombok ezt a lapos tömbökből álló, "beosztas-varazslo-v1"
    // formátumot használják (nem a Load/Save által kezelt, Windows-natív, egymásba ágyazott
    // szótárakból álló belső fájlformátumot) - így egy itt exportált fájl bármelyik platformon
    // (web, Android) importálható, és fordítva.

    private static (int Year, int Month) ParseMonthKey(string key)
    {
        var parts = key.Split('-');
        return (int.Parse(parts[0]), int.Parse(parts[1]));
    }

    private static string IdToString(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.String => el.GetString() ?? "",
        JsonValueKind.Number => el.GetRawText(),
        _ => el.ToString()
    };

    private static bool TryGetArrayAny(JsonElement root, out JsonElement result, params string[] names)
    {
        foreach (var n in names)
        {
            if (root.TryGetProperty(n, out result) && result.ValueKind == JsonValueKind.Array) return true;
        }
        result = default;
        return false;
    }

    /// <summary>Fájlból beolvas egy adatbázist a közös exportformátumból, anélkül hogy elmentené.</summary>
    public static AppDatabase ImportFromFile(string path)
    {
        var json = File.ReadAllText(path);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (!TryGetArrayAny(root, out var groupsEl, "groups") || !TryGetArrayAny(root, out var empEl, "employees"))
            throw new InvalidDataException("A fájl formátuma nem megfelelő (hiányzó munkacsoport/dolgozó lista).");

        var db = new AppDatabase();

        var groupIdMap = new Dictionary<string, long>();
        foreach (var jg in groupsEl.EnumerateArray())
        {
            var newId = NextId();
            var group = new WorkGroup
            {
                Id = newId,
                Name = jg.GetProperty("name").GetString() ?? "",
                Type = jg.GetProperty("type").GetString() ?? GroupTypes.General,
                DailyHours = jg.GetProperty("dailyHours").GetDouble(),
                StaffPerShift = jg.TryGetProperty("staffPerShift", out var spsEl) ? spsEl.GetInt32() : 0,
                MinRestHours = jg.TryGetProperty("minRestHours", out var mrhEl) ? mrhEl.GetInt32() : 24
            };
            if (jg.TryGetProperty("shiftTypes", out var stArr) && stArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var jst in stArr.EnumerateArray())
                {
                    group.ShiftTypes.Add(new ShiftType
                    {
                        Code = jst.GetProperty("code").GetString() ?? "",
                        Label = jst.GetProperty("label").GetString() ?? "",
                        Hours = jst.GetProperty("hours").GetDouble()
                    });
                }
            }
            db.Groups.Add(group);
            groupIdMap[IdToString(jg.GetProperty("id"))] = newId;
        }

        var employeeIdMap = new Dictionary<string, long>();
        foreach (var je in empEl.EnumerateArray())
        {
            var oldGroupId = IdToString(je.GetProperty("groupId"));
            if (!groupIdMap.TryGetValue(oldGroupId, out var newGroupId)) continue;
            var newId = NextId();

            var excluded = new List<string>();
            if (je.TryGetProperty("excludedShiftCodes", out var exEl))
            {
                if (exEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var codeEl in exEl.EnumerateArray())
                    {
                        var c = codeEl.GetString();
                        if (!string.IsNullOrEmpty(c)) excluded.Add(c);
                    }
                }
                else if (exEl.ValueKind == JsonValueKind.String)
                {
                    excluded.AddRange((exEl.GetString() ?? "")
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                }
            }

            db.Employees.Add(new Employee
            {
                Id = newId,
                Name = je.GetProperty("name").GetString() ?? "",
                GroupId = newGroupId,
                EmploymentFactor = je.TryGetProperty("employmentFactor", out var efEl) ? efEl.GetDouble() : 1.0,
                MaxVacationDays = je.GetProperty("maxVacationDays").GetInt32(),
                Notes = je.TryGetProperty("notes", out var notesEl) ? (notesEl.GetString() ?? "") : "",
                ExcludedShiftCodes = excluded
            });
            employeeIdMap[IdToString(je.GetProperty("id"))] = newId;
        }

        if (TryGetArrayAny(root, out var mhArr, "monthHours") && mhArr.GetArrayLength() > 0)
        {
            foreach (var jm in mhArr.EnumerateArray())
            {
                var month = jm.GetProperty("month").GetInt32();
                db.MonthHours[month] = new MonthHours { Hours = jm.GetProperty("hours").GetDouble(), Days = jm.GetProperty("days").GetInt32() };
            }
        }
        else
        {
            ResetMonthHours(db);
        }

        if (TryGetArrayAny(root, out var schArr, "schedule"))
        {
            foreach (var js in schArr.EnumerateArray())
            {
                if (!employeeIdMap.TryGetValue(IdToString(js.GetProperty("employeeId")), out var newEmpId)) continue;
                var key = AppDatabase.MonthKey(js.GetProperty("year").GetInt32(), js.GetProperty("month").GetInt32());
                if (!db.Schedule.TryGetValue(key, out var monthData)) { monthData = new(); db.Schedule[key] = monthData; }
                if (!monthData.TryGetValue(newEmpId, out var days)) { days = new(); monthData[newEmpId] = days; }
                days[js.GetProperty("day").GetInt32()] = js.GetProperty("code").GetString() ?? "";
            }
        }

        if (TryGetArrayAny(root, out var coArr, "carryOvers"))
        {
            foreach (var jc in coArr.EnumerateArray())
            {
                if (!employeeIdMap.TryGetValue(IdToString(jc.GetProperty("employeeId")), out var newEmpId)) continue;
                var key = AppDatabase.MonthKey(jc.GetProperty("year").GetInt32(), jc.GetProperty("month").GetInt32());
                if (!db.CarryOver.TryGetValue(key, out var monthData)) { monthData = new(); db.CarryOver[key] = monthData; }
                monthData[newEmpId] = jc.GetProperty("hours").GetDouble();
            }
        }

        if (TryGetArrayAny(root, out var extraArr, "holidaysExtra", "holidayExtra"))
        {
            foreach (var jh in extraArr.EnumerateArray())
            {
                var year = jh.GetProperty("year").GetInt32();
                if (!db.HolidaysExtra.TryGetValue(year, out var dict)) { dict = new(); db.HolidaysExtra[year] = dict; }
                dict[jh.GetProperty("date").GetString() ?? ""] = jh.GetProperty("name").GetString() ?? "";
            }
        }

        if (TryGetArrayAny(root, out var removedArr, "holidaysRemoved", "holidayRemoved"))
        {
            foreach (var jh in removedArr.EnumerateArray())
            {
                var year = jh.GetProperty("year").GetInt32();
                if (!db.HolidaysRemoved.TryGetValue(year, out var list)) { list = new(); db.HolidaysRemoved[year] = list; }
                list.Add(jh.GetProperty("date").GetString() ?? "");
            }
        }

        EnsureSeeded(db);
        return db;
    }

    /// <summary>Adatbázis mentése a közös exportformátumba (lásd fent).</summary>
    public static void ExportToFile(AppDatabase db, string path)
    {
        var root = new JsonObject
        {
            ["exportFormat"] = "beosztas-varazslo-v1",
            ["generatedBy"] = "windows",
            ["generatedAt"] = DateTime.UtcNow.ToString("o")
        };

        var monthHoursArr = new JsonArray();
        foreach (var kv in db.MonthHours.OrderBy(k => k.Key))
            monthHoursArr.Add(new JsonObject { ["month"] = kv.Key, ["hours"] = kv.Value.Hours, ["days"] = kv.Value.Days });
        root["monthHours"] = monthHoursArr;

        var groupsArr = new JsonArray();
        foreach (var g in db.Groups)
        {
            var shiftTypesArr = new JsonArray();
            foreach (var st in g.ShiftTypes)
                shiftTypesArr.Add(new JsonObject { ["code"] = st.Code, ["label"] = st.Label, ["hours"] = st.Hours });
            groupsArr.Add(new JsonObject
            {
                ["id"] = g.Id.ToString(),
                ["name"] = g.Name,
                ["type"] = g.Type,
                ["dailyHours"] = g.DailyHours,
                ["staffPerShift"] = g.StaffPerShift,
                ["minRestHours"] = g.MinRestHours,
                ["shiftTypes"] = shiftTypesArr
            });
        }
        root["groups"] = groupsArr;

        var empArr = new JsonArray();
        foreach (var e in db.Employees)
        {
            var exclArr = new JsonArray();
            foreach (var code in e.ExcludedShiftCodes) exclArr.Add(code);
            empArr.Add(new JsonObject
            {
                ["id"] = e.Id.ToString(),
                ["name"] = e.Name,
                ["groupId"] = e.GroupId.ToString(),
                ["employmentFactor"] = e.EmploymentFactor,
                ["maxVacationDays"] = e.MaxVacationDays,
                ["notes"] = e.Notes,
                ["excludedShiftCodes"] = exclArr
            });
        }
        root["employees"] = empArr;

        var scheduleArr = new JsonArray();
        foreach (var monthKv in db.Schedule)
        {
            var (year, month) = ParseMonthKey(monthKv.Key);
            foreach (var empKv in monthKv.Value)
                foreach (var dayKv in empKv.Value)
                    scheduleArr.Add(new JsonObject
                    {
                        ["employeeId"] = empKv.Key.ToString(),
                        ["year"] = year,
                        ["month"] = month,
                        ["day"] = dayKv.Key,
                        ["code"] = dayKv.Value
                    });
        }
        root["schedule"] = scheduleArr;

        var carryArr = new JsonArray();
        foreach (var monthKv in db.CarryOver)
        {
            var (year, month) = ParseMonthKey(monthKv.Key);
            foreach (var empKv in monthKv.Value)
                carryArr.Add(new JsonObject { ["employeeId"] = empKv.Key.ToString(), ["year"] = year, ["month"] = month, ["hours"] = empKv.Value });
        }
        root["carryOvers"] = carryArr;

        var extraArr = new JsonArray();
        foreach (var yearKv in db.HolidaysExtra)
            foreach (var dateKv in yearKv.Value)
                extraArr.Add(new JsonObject { ["year"] = yearKv.Key, ["date"] = dateKv.Key, ["name"] = dateKv.Value });
        root["holidaysExtra"] = extraArr;

        var removedArr = new JsonArray();
        foreach (var yearKv in db.HolidaysRemoved)
            foreach (var date in yearKv.Value)
                removedArr.Add(new JsonObject { ["year"] = yearKv.Key, ["date"] = date });
        root["holidaysRemoved"] = removedArr;

        var writerOptions = new JsonWriterOptions
        {
            Indented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, writerOptions))
        {
            root.WriteTo(writer);
        }
        File.WriteAllBytes(path, stream.ToArray());
    }

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
