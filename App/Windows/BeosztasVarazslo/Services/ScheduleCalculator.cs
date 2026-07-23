using BeosztasVarazslo.Models;

namespace BeosztasVarazslo.Services;

public enum CellKind { Work, Vacation, Absence, Rest, Empty, NonWork }

public record CellInfo(double Hours, CellKind Kind, string? Code = null);

public class MonthSummary
{
    public double BaseHours { get; init; }
    public double EmploymentFactor { get; init; }
    public double RequiredFull { get; init; }
    public int VacationDays { get; init; }
    public int AbsenceDays { get; init; }
    public int WorkDays { get; init; }
    public double VacationHours { get; init; }
    public double AbsenceHours { get; init; }
    public double CarryIn { get; init; }
    public double EffectiveRequired { get; init; }
    public double ActualHours { get; init; }
    public double Balance { get; init; }
}

/// <summary>Óraszám-számítási motor: kötelező havi óraszám, ledolgozott óra, maradvány.</summary>
public static class ScheduleCalculator
{
    public static int DaysInMonth(int year, int month) => DateTime.DaysInMonth(year, month);

    public static bool IsWeekend(int year, int month, int day)
    {
        var dow = new DateTime(year, month, day).DayOfWeek;
        return dow is DayOfWeek.Saturday or DayOfWeek.Sunday;
    }

    public static string IsoDate(int year, int month, int day) => $"{year:D4}-{month:D2}-{day:D2}";

    public static bool IsHoliday(IReadOnlyDictionary<string, string> holidayMap, int year, int month, int day) =>
        holidayMap.ContainsKey(IsoDate(year, month, day));

    public static bool IsOfficeWorkday(IReadOnlyDictionary<string, string> holidayMap, int year, int month, int day) =>
        !IsWeekend(year, month, day) && !IsHoliday(holidayMap, year, month, day);

    public static CellInfo CellHours(string groupType, double groupDailyHours, List<ShiftType> shiftTypes, string? code, bool isOfficeWorkday)
    {
        if (groupType == GroupTypes.Office)
        {
            if (!isOfficeWorkday) return new CellInfo(0, CellKind.NonWork);
            return code switch
            {
                ShiftCodes.Vacation => new CellInfo(0, CellKind.Vacation),
                ShiftCodes.Absence or ShiftCodes.Sick => new CellInfo(0, CellKind.Absence),
                _ => new CellInfo(groupDailyHours, CellKind.Work, "M")
            };
        }

        if (string.IsNullOrEmpty(code)) return new CellInfo(0, CellKind.Empty);
        switch (code)
        {
            case ShiftCodes.Vacation: return new CellInfo(0, CellKind.Vacation);
            case ShiftCodes.Absence: case ShiftCodes.Sick: return new CellInfo(0, CellKind.Absence);
            case ShiftCodes.Rest: return new CellInfo(0, CellKind.Rest);
            default:
                var st = shiftTypes.Find(s => s.Code == code);
                return st != null ? new CellInfo(st.Hours, CellKind.Work, st.Code) : new CellInfo(0, CellKind.Empty);
        }
    }

    public static MonthSummary SummarizeMonth(
        string groupType, double groupDailyHours, List<ShiftType> shiftTypes,
        double employmentFactor, double baseMonthHours,
        IReadOnlyDictionary<int, string> codesByDay, IReadOnlyDictionary<string, string> holidayMap,
        double carryIn, int year, int month)
    {
        int dim = DaysInMonth(year, month);
        double actualHours = 0;
        int vacationDays = 0, absenceDays = 0, workDays = 0;

        for (int d = 1; d <= dim; d++)
        {
            codesByDay.TryGetValue(d, out var code);
            bool officeWorkday = IsOfficeWorkday(holidayMap, year, month, d);
            var info = CellHours(groupType, groupDailyHours, shiftTypes, code, officeWorkday);
            switch (info.Kind)
            {
                case CellKind.Work: actualHours += info.Hours; workDays++; break;
                case CellKind.Vacation: vacationDays++; break;
                case CellKind.Absence: absenceDays++; break;
            }
        }

        double requiredFull = baseMonthHours * employmentFactor;
        double vacationHours = vacationDays * groupDailyHours;
        double absenceHours = absenceDays * groupDailyHours;
        double effectiveRequired = requiredFull - vacationHours - absenceHours - carryIn;
        double balance = actualHours - effectiveRequired;

        return new MonthSummary
        {
            BaseHours = baseMonthHours, EmploymentFactor = employmentFactor, RequiredFull = requiredFull,
            VacationDays = vacationDays, AbsenceDays = absenceDays, WorkDays = workDays,
            VacationHours = vacationHours, AbsenceHours = absenceHours, CarryIn = carryIn,
            EffectiveRequired = effectiveRequired, ActualHours = actualHours, Balance = balance
        };
    }

    /// <summary>Egy adott napon, adott műszaktípus-kódonként hány dolgozó van beosztva.</summary>
    public static Dictionary<string, int> ShiftCoverage(List<ShiftType> shiftTypes, IEnumerable<string?> codesOfEmployeesThatDay)
    {
        var counts = shiftTypes.ToDictionary(s => s.Code, _ => 0);
        foreach (var code in codesOfEmployeesThatDay)
        {
            if (code != null && counts.ContainsKey(code)) counts[code]++;
        }
        return counts;
    }

    /// <summary>A cellára kattintva választható kódok (kód, megjelenített címke) párokban.</summary>
    public static List<(string Code, string Label)> CellOptions(string groupType, List<ShiftType> shiftTypes)
    {
        if (groupType == GroupTypes.Office)
        {
            return new List<(string, string)>
            {
                ("", "Munka"), (ShiftCodes.Vacation, "SZ – Szabadság"),
                (ShiftCodes.Sick, "BSZ – Beteg szabadság"), (ShiftCodes.Absence, "H – Hiányzás")
            };
        }
        var opts = new List<(string, string)> { ("", "—") };
        foreach (var st in shiftTypes) opts.Add((st.Code, $"{st.Code} – {st.Label}"));
        opts.Add((ShiftCodes.Vacation, "SZ – Szabadság"));
        opts.Add((ShiftCodes.Sick, "BSZ – Beteg szabadság"));
        opts.Add((ShiftCodes.Absence, "H – Hiányzás"));
        opts.Add((ShiftCodes.Rest, "P – Pihenőnap"));
        return opts;
    }
}
