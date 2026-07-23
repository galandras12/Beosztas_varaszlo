using BeosztasVarazslo.Models;

namespace BeosztasVarazslo.Services;

/// <summary>Összeállítja a nyomtatható táblázat adatait (munkakör szerint ABC sorrendben) egy adott hónapra.</summary>
public static class PrintDocumentBuilder
{
    private static readonly string[] DowLetters = { "V", "H", "K", "Sze", "Cs", "P", "Szo" };

    public static PrintDocument Build(AppRepository repo, int year, int month)
    {
        var groups = repo.GetGroupsSorted();
        var employees = repo.GetEmployeesSorted();
        var holidayMap = repo.GetHolidayMap(year);
        var baseHours = repo.GetMonthHours(month)?.Hours ?? 0;
        var dim = ScheduleCalculator.DaysInMonth(year, month);

        var dayHeaders = new List<string>();
        for (int d = 1; d <= dim; d++)
        {
            var dow = DowLetters[(int)new DateTime(year, month, d).DayOfWeek];
            dayHeaders.Add($"{d}\n{dow}");
        }

        var rows = new List<PrintRow>();
        foreach (var group in groups)
        {
            var groupEmployees = employees.Where(e => e.GroupId == group.Id)
                .OrderBy(e => e.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
            if (groupEmployees.Count == 0) continue;

            rows.Add(new PrintRow(group.Name, true, new List<string>(), ""));

            foreach (var emp in groupEmployees)
            {
                var codes = repo.GetMonthCodes(emp.Id, year, month);
                var carryIn = repo.GetCarryIn(emp.Id, year, month);
                var summary = ScheduleCalculator.SummarizeMonth(
                    group.Type, group.DailyHours, group.ShiftTypes, emp.EmploymentFactor, baseHours,
                    codes, holidayMap, carryIn, year, month);

                var dayTexts = new List<string>();
                for (int d = 1; d <= dim; d++)
                {
                    if (group.Type == GroupTypes.Office)
                    {
                        dayTexts.Add(!ScheduleCalculator.IsOfficeWorkday(holidayMap, year, month, d)
                            ? "·"
                            : codes.GetValueOrDefault(d, ""));
                    }
                    else
                    {
                        dayTexts.Add(codes.GetValueOrDefault(d, ""));
                    }
                }

                rows.Add(new PrintRow(emp.Name, false, dayTexts, summary.Balance.ToString("0.0")));
            }
        }

        return new PrintDocument(
            $"{AppDatabaseService.MonthNames[month - 1]} {year} – {month}. hónap – havi munkabeosztás",
            dayHeaders, rows);
    }
}
