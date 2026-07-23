using BeosztasVarazslo.Models;

namespace BeosztasVarazslo.Services;

/// <summary>
/// Magas szintű adatelérés a memóriában tartott <see cref="AppDatabase"/> fölött.
/// Minden módosító művelet után azonnal lementi a JSON adatbázist (ugyanaz a viselkedés,
/// mint a böngészős/Android verzióban).
/// </summary>
public class AppRepository
{
    public AppDatabase Db { get; private set; }

    public AppRepository()
    {
        Db = AppDatabaseService.Load();
    }

    private void Save() => AppDatabaseService.Save(Db);

    public void ResetAll()
    {
        Db = AppDatabaseService.CreateDefault();
        Save();
    }

    public void ReplaceDatabase(AppDatabase newDb)
    {
        Db = newDb;
        Save();
    }

    // ---------- Munkacsoportok ----------

    public List<WorkGroup> GetGroupsSorted() =>
        Db.Groups.OrderBy(g => g.Name, StringComparer.CurrentCultureIgnoreCase).ToList();

    public int EmployeeCountForGroup(long groupId) => Db.Employees.Count(e => e.GroupId == groupId);

    public void SaveGroup(WorkGroup group)
    {
        if (group.Id == 0)
        {
            group.Id = AppDatabaseService.NextId();
            Db.Groups.Add(group);
        }
        else
        {
            var idx = Db.Groups.FindIndex(g => g.Id == group.Id);
            if (idx >= 0) Db.Groups[idx] = group;
            else Db.Groups.Add(group);
        }
        Save();
    }

    /// <returns>true, ha törölve lett; false, ha vannak hozzá rendelt dolgozók.</returns>
    public bool DeleteGroup(WorkGroup group)
    {
        if (EmployeeCountForGroup(group.Id) > 0) return false;
        Db.Groups.RemoveAll(g => g.Id == group.Id);
        Save();
        return true;
    }

    // ---------- Dolgozók ----------

    public List<Employee> GetEmployeesSorted() =>
        Db.Employees.OrderBy(e => e.Name, StringComparer.CurrentCultureIgnoreCase).ToList();

    public List<Employee> GetEmployeesForGroup(long groupId) =>
        Db.Employees.Where(e => e.GroupId == groupId).OrderBy(e => e.Name, StringComparer.CurrentCultureIgnoreCase).ToList();

    public void SaveEmployee(Employee employee)
    {
        if (employee.Id == 0)
        {
            employee.Id = AppDatabaseService.NextId();
            Db.Employees.Add(employee);
        }
        else
        {
            var idx = Db.Employees.FindIndex(e => e.Id == employee.Id);
            if (idx >= 0) Db.Employees[idx] = employee;
            else Db.Employees.Add(employee);
        }
        Save();
    }

    public void DeleteEmployee(Employee employee)
    {
        Db.Employees.RemoveAll(e => e.Id == employee.Id);
        foreach (var monthData in Db.Schedule.Values) monthData.Remove(employee.Id);
        foreach (var monthData in Db.CarryOver.Values) monthData.Remove(employee.Id);
        Save();
    }

    // ---------- Havi óraszám tábla ----------

    public MonthHours? GetMonthHours(int month) => Db.MonthHours.GetValueOrDefault(month);

    public void SetMonthHours(int month, double hours, int days)
    {
        Db.MonthHours[month] = new MonthHours { Hours = hours, Days = days };
        Save();
    }

    public void ResetMonthHoursToDefault()
    {
        AppDatabaseService.ResetMonthHours(Db);
        Save();
    }

    // ---------- Ünnepnapok ----------

    public Dictionary<string, string> GetHolidayMap(int year)
    {
        var map = HungarianHolidays.DefaultHolidays(year);
        if (Db.HolidaysRemoved.TryGetValue(year, out var removed))
            foreach (var date in removed) map.Remove(date);
        if (Db.HolidaysExtra.TryGetValue(year, out var extra))
            foreach (var (date, name) in extra) map[date] = name;
        return map;
    }

    public void AddExtraHoliday(int year, string date, string name)
    {
        if (!Db.HolidaysExtra.TryGetValue(year, out var dict))
        {
            dict = new Dictionary<string, string>();
            Db.HolidaysExtra[year] = dict;
        }
        dict[date] = name;
        Save();
    }

    public void DeleteExtraHoliday(int year, string date)
    {
        if (Db.HolidaysExtra.TryGetValue(year, out var dict)) dict.Remove(date);
        Save();
    }

    public void SetHolidayRemoved(int year, string date, bool removed)
    {
        if (!Db.HolidaysRemoved.TryGetValue(year, out var list))
        {
            list = new List<string>();
            Db.HolidaysRemoved[year] = list;
        }
        if (removed) { if (!list.Contains(date)) list.Add(date); }
        else list.Remove(date);
        Save();
    }

    public HashSet<string> GetRemovedHolidays(int year) =>
        Db.HolidaysRemoved.TryGetValue(year, out var list) ? list.ToHashSet() : new HashSet<string>();

    public Dictionary<string, string> GetExtraHolidays(int year) =>
        Db.HolidaysExtra.TryGetValue(year, out var dict) ? dict : new Dictionary<string, string>();

    // ---------- Beosztás ----------

    public Dictionary<int, string> GetMonthCodes(long employeeId, int year, int month)
    {
        var key = AppDatabase.MonthKey(year, month);
        if (Db.Schedule.TryGetValue(key, out var monthData) && monthData.TryGetValue(employeeId, out var days))
            return new Dictionary<int, string>(days);
        return new Dictionary<int, string>();
    }

    /// <summary>employeeId -> (nap -> kód), egy adott hónapra, az összes dolgozóra (lefedettség-számításhoz).</summary>
    public Dictionary<long, Dictionary<int, string>> GetMonthCodesForAll(int year, int month)
    {
        var key = AppDatabase.MonthKey(year, month);
        return Db.Schedule.TryGetValue(key, out var monthData)
            ? monthData.ToDictionary(kv => kv.Key, kv => new Dictionary<int, string>(kv.Value))
            : new Dictionary<long, Dictionary<int, string>>();
    }

    public void SetCell(long employeeId, int year, int month, int day, string code)
    {
        var key = AppDatabase.MonthKey(year, month);
        if (!Db.Schedule.TryGetValue(key, out var monthData))
        {
            monthData = new Dictionary<long, Dictionary<int, string>>();
            Db.Schedule[key] = monthData;
        }
        if (!monthData.TryGetValue(employeeId, out var days))
        {
            days = new Dictionary<int, string>();
            monthData[employeeId] = days;
        }
        if (string.IsNullOrEmpty(code)) days.Remove(day);
        else days[day] = code;
        Save();
    }

    public double GetCarryIn(long employeeId, int year, int month)
    {
        var key = AppDatabase.MonthKey(year, month);
        if (Db.CarryOver.TryGetValue(key, out var monthData) && monthData.TryGetValue(employeeId, out var v))
            return v;
        return 0;
    }

    public void SetCarryIn(long employeeId, int year, int month, double hours)
    {
        var key = AppDatabase.MonthKey(year, month);
        if (!Db.CarryOver.TryGetValue(key, out var monthData))
        {
            monthData = new Dictionary<long, double>();
            Db.CarryOver[key] = monthData;
        }
        monthData[employeeId] = hours;
        Save();
    }

    public int YearVacationUsed(long employeeId, int year)
    {
        int total = 0;
        for (int m = 1; m <= 12; m++)
        {
            var key = AppDatabase.MonthKey(year, m);
            if (!Db.Schedule.TryGetValue(key, out var monthData)) continue;
            if (!monthData.TryGetValue(employeeId, out var days)) continue;
            total += days.Values.Count(c => c == ShiftCodes.Vacation);
        }
        return total;
    }

    /// <summary>
    /// Hirtelen beteg szabadság esetén automatikus helyettes-keresés: az adott napon szabad
    /// (aznapra még be nem osztott) csoporttagok közül azt választja, akinek eddig a legkevesebb
    /// ledolgozott órája van ebben a hónapban, és őt állítja be a beteg dolgozó műszakjára.
    /// </summary>
    /// <returns>A kiválasztott helyettesítő, vagy null, ha nincs elérhető szabad dolgozó.</returns>
    public Employee? FindSickSubstitute(WorkGroup group, List<Employee> employees, int year, int month, int day, long sickEmployeeId, string shiftCode)
    {
        var holidayMap = GetHolidayMap(year);
        var baseHours = GetMonthHours(month)?.Hours ?? 0;

        Employee? best = null;
        double bestHours = double.MaxValue;
        foreach (var emp in employees)
        {
            if (emp.Id == sickEmployeeId) continue;
            var codes = GetMonthCodes(emp.Id, year, month);
            if (codes.TryGetValue(day, out var existing) && !string.IsNullOrEmpty(existing)) continue;

            var carryIn = GetCarryIn(emp.Id, year, month);
            var summary = ScheduleCalculator.SummarizeMonth(
                group.Type, group.DailyHours, group.ShiftTypes, emp.EmploymentFactor, baseHours,
                codes, holidayMap, carryIn, year, month);
            if (summary.ActualHours < bestHours)
            {
                bestHours = summary.ActualHours;
                best = emp;
            }
        }
        if (best != null) SetCell(best.Id, year, month, day, shiftCode);
        return best;
    }

    public record AutoFillShortfall(string Group, int Day, string ShiftLabel, int Needed, int Assigned);
    public record AutoFillResult(int FilledCells, List<AutoFillShortfall> Shortfalls);

    /// <summary>
    /// Automatikus beosztás-kitöltő. Csak ÜRES cellákba ír - meglévő (kézzel beírt vagy korábban
    /// generált) kódokat sosem ír felül.
    /// - Irodai (hétfő-péntek) csoportoknál: minden munkanapon minden dolgozóhoz "M" kódot ír.
    /// - Egymást váltó, létszám-figyelt (StaffPerShift > 0) csoportoknál: napról napra,
    ///   műszaktípusonként annyi szabad, a csoport MinRestHours pihenőidejét betartó dolgozót
    ///   jelöl ki, ameddig a szükséges létszám meg nem telik - a legkevesebb eddig ledolgozott
    ///   órájú, egyformaság esetén névsor szerinti dolgozókat részesítve előnyben. Ha nincs elég
    ///   szabad/pihent dolgozó, annyit oszt be, amennyi van, és a hiányt jelzi.
    /// </summary>
    public AutoFillResult AutoFillMonth(int year, int month)
    {
        var dim = ScheduleCalculator.DaysInMonth(year, month);
        var holidayMap = GetHolidayMap(year);
        int filledCells = 0;
        var shortfalls = new List<AutoFillShortfall>();

        foreach (var group in GetGroupsSorted())
        {
            var groupEmployees = GetEmployeesForGroup(group.Id);
            if (groupEmployees.Count == 0) continue;

            if (group.Type == GroupTypes.Office)
            {
                foreach (var emp in groupEmployees)
                {
                    var codes = GetMonthCodes(emp.Id, year, month);
                    for (int d = 1; d <= dim; d++)
                    {
                        if (!ScheduleCalculator.IsOfficeWorkday(holidayMap, year, month, d)) continue;
                        if (codes.TryGetValue(d, out var existing) && !string.IsNullOrEmpty(existing)) continue;
                        SetCell(emp.Id, year, month, d, "M");
                        filledCells++;
                    }
                }
                continue;
            }

            if (group.StaffPerShift > 0 && group.ShiftTypes.Count > 0)
            {
                int minRestDays = (int)Math.Ceiling(group.MinRestHours / 24.0);
                var codesByEmployee = groupEmployees.ToDictionary(e => e.Id, e => GetMonthCodes(e.Id, year, month));
                var lastWorkedDay = new Dictionary<long, int>();
                var shiftCount = new Dictionary<long, int>();

                foreach (var emp in groupEmployees)
                {
                    lastWorkedDay[emp.Id] = int.MinValue;
                    shiftCount[emp.Id] = 0;
                    var codes = codesByEmployee[emp.Id];
                    for (int d = 1; d <= dim; d++)
                    {
                        if (codes.TryGetValue(d, out var code) && group.ShiftTypes.Any(st => st.Code == code))
                        {
                            lastWorkedDay[emp.Id] = d;
                            shiftCount[emp.Id]++;
                        }
                    }
                }

                for (int d = 1; d <= dim; d++)
                {
                    var assignedToday = new HashSet<long>();
                    foreach (var emp in groupEmployees)
                        if (codesByEmployee[emp.Id].TryGetValue(d, out var existing) && !string.IsNullOrEmpty(existing))
                            assignedToday.Add(emp.Id);

                    foreach (var st in group.ShiftTypes)
                    {
                        int existingCount = groupEmployees.Count(e => codesByEmployee[e.Id].TryGetValue(d, out var c) && c == st.Code);
                        int needed = Math.Max(0, group.StaffPerShift - existingCount);
                        if (needed == 0) continue;

                        var candidates = groupEmployees.Where(emp =>
                        {
                            if (assignedToday.Contains(emp.Id)) return false;
                            if (codesByEmployee[emp.Id].TryGetValue(d, out var existing) && !string.IsNullOrEmpty(existing)) return false;
                            var last = lastWorkedDay[emp.Id];
                            return last == int.MinValue || (d - last) > minRestDays;
                        })
                        .OrderBy(e => shiftCount[e.Id])
                        .ThenBy(e => e.Name, StringComparer.CurrentCultureIgnoreCase)
                        .ToList();

                        var toAssign = candidates.Take(needed).ToList();
                        foreach (var emp in toAssign)
                        {
                            SetCell(emp.Id, year, month, d, st.Code);
                            codesByEmployee[emp.Id][d] = st.Code;
                            assignedToday.Add(emp.Id);
                            lastWorkedDay[emp.Id] = d;
                            shiftCount[emp.Id]++;
                            filledCells++;
                        }

                        if (toAssign.Count < needed)
                            shortfalls.Add(new AutoFillShortfall(group.Name, d, $"{st.Label} ({st.Code})", group.StaffPerShift, existingCount + toAssign.Count));
                    }
                }
            }
        }

        return new AutoFillResult(filledCells, shortfalls);
    }
}
