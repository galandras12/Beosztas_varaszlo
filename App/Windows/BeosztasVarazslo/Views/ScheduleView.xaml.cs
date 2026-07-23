using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using BeosztasVarazslo.Models;
using BeosztasVarazslo.Services;

namespace BeosztasVarazslo.Views;

public partial class ScheduleView : UserControl, IRefreshableView
{
    private readonly AppRepository _repo;
    private int _selYear = DateTime.Now.Year;
    private int _selMonth = DateTime.Now.Month;
    private List<Button> _monthButtons = new();

    private static readonly string[] DowLetters = { "V", "H", "K", "Sze", "Cs", "P", "Szo" };

    private const double NameColWidth = 170;
    private const double DayColWidth = 42;
    private const double StatColWidth = 92;
    private const double RowHeight = 34;

    public ScheduleView(AppRepository repo)
    {
        InitializeComponent();
        _repo = repo;
        TxtYear.Text = _selYear.ToString();

        _monthButtons = new List<Button>
        {
            BtnMonth1, BtnMonth2, BtnMonth3, BtnMonth4, BtnMonth5, BtnMonth6,
            BtnMonth7, BtnMonth8, BtnMonth9, BtnMonth10, BtnMonth11, BtnMonth12
        };

        HighlightMonthButtons();
        Refresh();
    }

    public void Refresh() => RebuildGrid();

    private void TxtYear_LostFocus(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(TxtYear.Text, out var y)) { _selYear = y; RebuildGrid(); }
    }

    private void MonthButton_Click(object sender, RoutedEventArgs e)
    {
        var idx = _monthButtons.IndexOf((Button)sender);
        if (idx < 0) return;
        _selMonth = idx + 1;
        HighlightMonthButtons();
        RebuildGrid();
    }

    private void HighlightMonthButtons()
    {
        for (int i = 0; i < _monthButtons.Count; i++)
        {
            bool selected = (i + 1) == _selMonth;
            _monthButtons[i].Foreground = (Brush)FindResource(selected ? "AccentBrush" : "TextSecondaryBrush");
            _monthButtons[i].FontWeight = selected ? FontWeights.SemiBold : FontWeights.Normal;
        }
    }

    private Border CellBorder(string text, double width, Brush? background = null, bool bold = false, Brush? foreground = null)
    {
        return new Border
        {
            Width = width,
            Height = RowHeight,
            Background = background ?? Brushes.Transparent,
            Child = new TextBlock
            {
                Text = text,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                FontSize = 11,
                FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
                Foreground = foreground ?? (Brush)FindResource("TextPrimaryBrush")
            }
        };
    }

    private void RebuildGrid()
    {
        TxtScheduleTitle.Text = $"Beosztás – {AppDatabaseService.MonthNames[_selMonth - 1]} {_selYear} ({_selMonth}. hónap)";

        var groups = _repo.GetGroupsSorted();
        var employees = _repo.GetEmployeesSorted();
        var holidayMap = _repo.GetHolidayMap(_selYear);
        var baseHours = _repo.GetMonthHours(_selMonth)?.Hours ?? 0;
        var dim = ScheduleCalculator.DaysInMonth(_selYear, _selMonth);

        NamesColumn.Children.Clear();
        DaysColumn.Children.Clear();

        double totalRowWidth = dim * DayColWidth + 5 * StatColWidth;

        // Fejléc
        NamesColumn.Children.Add(HeaderCellBorder("Dolgozó", NameColWidth, leftAlign: true));
        var headerRow = new StackPanel { Orientation = Orientation.Horizontal };
        for (int d = 1; d <= dim; d++)
        {
            var dow = DowLetters[(int)new DateTime(_selYear, _selMonth, d).DayOfWeek];
            headerRow.Children.Add(HeaderCellBorder($"{d}\n{dow}", DayColWidth));
        }
        foreach (var label in new[] { "Köt.ó", "Ledolg.ó", "Szab.nap", "Bejövő ó.", "Egyenleg" })
            headerRow.Children.Add(HeaderCellBorder(label, StatColWidth, bold: true));
        DaysColumn.Children.Add(headerRow);

        foreach (var group in groups)
        {
            var groupEmployees = employees.Where(e => e.GroupId == group.Id)
                .OrderBy(e => e.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
            if (groupEmployees.Count == 0) continue;

            NamesColumn.Children.Add(new Border
            {
                Width = NameColWidth, Height = RowHeight, Background = (Brush)FindResource("WarningBrush"),
                Child = new TextBlock
                {
                    Text = group.Name, Foreground = Brushes.White, FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0)
                }
            });
            var groupHeaderRow = new StackPanel { Orientation = Orientation.Horizontal };
            groupHeaderRow.Children.Add(new Border
            {
                Width = totalRowWidth, Height = RowHeight, Background = (Brush)FindResource("WarningBrush")
            });
            DaysColumn.Children.Add(groupHeaderRow);

            foreach (var emp in groupEmployees)
                AddEmployeeRow(emp, group, holidayMap, baseHours, dim);
        }

        RebuildCoverage(groups, employees);
    }

    private Border HeaderCellBorder(string text, double width, bool bold = true, bool leftAlign = false)
    {
        return new Border
        {
            Width = width,
            Height = RowHeight,
            Background = (Brush)FindResource("BorderBrush"),
            Child = new TextBlock
            {
                Text = text,
                TextAlignment = leftAlign ? TextAlignment.Left : TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = leftAlign ? new Thickness(8, 0, 0, 0) : new Thickness(0),
                FontSize = 11,
                FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
                Foreground = (Brush)FindResource("TextPrimaryBrush")
            }
        };
    }

    private void AddEmployeeRow(Employee emp, WorkGroup group, Dictionary<string, string> holidayMap, double baseHours, int dim)
    {
        var codes = _repo.GetMonthCodes(emp.Id, _selYear, _selMonth);
        var carryIn = _repo.GetCarryIn(emp.Id, _selYear, _selMonth);
        var summary = ScheduleCalculator.SummarizeMonth(
            group.Type, group.DailyHours, group.ShiftTypes, emp.EmploymentFactor, baseHours,
            codes, holidayMap, carryIn, _selYear, _selMonth);

        NamesColumn.Children.Add(new Border
        {
            Width = NameColWidth, Height = RowHeight,
            Child = new TextBlock
            {
                Text = emp.Name, VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0), Foreground = (Brush)FindResource("TextPrimaryBrush")
            }
        });

        var row = new StackPanel { Orientation = Orientation.Horizontal };
        for (int d = 1; d <= dim; d++)
        {
            bool isOfficeWorkday = ScheduleCalculator.IsOfficeWorkday(holidayMap, _selYear, _selMonth, d);
            bool isWeekend = ScheduleCalculator.IsWeekend(_selYear, _selMonth, d);
            bool isHoliday = ScheduleCalculator.IsHoliday(holidayMap, _selYear, _selMonth, d);

            if (group.Type == GroupTypes.Office && !isOfficeWorkday)
            {
                var bg = (Brush)FindResource(isHoliday ? "HolidayBrush" : "WeekendBrush");
                row.Children.Add(CellBorder(isHoliday ? "Ü" : "·", DayColWidth, bg));
                continue;
            }

            codes.TryGetValue(d, out var code);
            code ??= "";
            Brush? cellBg = code switch
            {
                ShiftCodes.Vacation => (Brush)FindResource("VacationBrush"),
                ShiftCodes.Absence => (Brush)FindResource("AbsenceBrush"),
                ShiftCodes.Rest => (Brush)FindResource("RestBrush"),
                _ => isHoliday ? (Brush)FindResource("HolidayBrush") : isWeekend ? (Brush)FindResource("WeekendBrush") : null
            };

            var btn = new Button
            {
                Content = code,
                Width = DayColWidth,
                Height = RowHeight,
                Padding = new Thickness(0),
                Margin = new Thickness(0),
                BorderThickness = new Thickness(0),
                FontSize = 11,
                Background = cellBg ?? Brushes.Transparent
            };
            var day = d;
            var employee = emp;
            var currentGroup = group;
            btn.Click += (_, _) => OpenCellPicker(employee, currentGroup, day, code);
            row.Children.Add(btn);
        }

        row.Children.Add(CellBorder(FormatNum(summary.RequiredFull), StatColWidth));
        row.Children.Add(CellBorder(FormatNum(summary.ActualHours), StatColWidth));
        row.Children.Add(CellBorder(summary.VacationDays.ToString(), StatColWidth));

        var carryBox = new TextBox
        {
            Text = FormatNum(carryIn),
            Width = StatColWidth - 8,
            Height = RowHeight - 8,
            Margin = new Thickness(4),
            TextAlignment = TextAlignment.Center,
            FontSize = 11,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        carryBox.LostFocus += (_, _) =>
        {
            if (double.TryParse(carryBox.Text, out var v))
            {
                _repo.SetCarryIn(emp.Id, _selYear, _selMonth, v);
                RebuildGrid();
            }
        };
        var carryWrap = new Border { Width = StatColWidth, Height = RowHeight, Child = carryBox };
        row.Children.Add(carryWrap);

        var balanceBrush = summary.Balance < 0 ? (Brush)FindResource("DangerBrush") : (Brush)FindResource("SuccessBrush");
        row.Children.Add(CellBorder(FormatNum(summary.Balance), StatColWidth, bold: true, foreground: balanceBrush));

        DaysColumn.Children.Add(row);
    }

    private static string FormatNum(double d) => d.ToString("0.0");

    private void OpenCellPicker(Employee employee, WorkGroup group, int day, string oldCode)
    {
        var options = ScheduleCalculator.CellOptions(group.Type, group.ShiftTypes);
        var dlg = new CellPickerWindow($"{employee.Name} – {day}. nap", options) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() != true || dlg.SelectedCode == null) return;

        var newCode = dlg.SelectedCode;
        if (newCode == ShiftCodes.Vacation && oldCode != ShiftCodes.Vacation)
        {
            var used = _repo.YearVacationUsed(employee.Id, _selYear);
            if (used + 1 > employee.MaxVacationDays)
            {
                MessageBox.Show(Window.GetWindow(this),
                    $"{employee.Name} már elérte a max. kiadható szabadság napok számát ({employee.MaxVacationDays} nap, {_selYear}). Nem jelölhető ki több szabadnap.",
                    "Beosztás Varázsló", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }
        _repo.SetCell(employee.Id, _selYear, _selMonth, day, newCode);
        RebuildGrid();
    }

    private void BtnCopyPrevCarry_Click(object sender, RoutedEventArgs e)
    {
        int prevYear = _selYear, prevMonth = _selMonth - 1;
        if (prevMonth < 1) { prevMonth = 12; prevYear -= 1; }

        var groups = _repo.GetGroupsSorted();
        var employees = _repo.GetEmployeesSorted();
        var holidayMap = _repo.GetHolidayMap(prevYear);
        var baseHours = _repo.GetMonthHours(prevMonth)?.Hours ?? 0;

        foreach (var emp in employees)
        {
            var group = groups.FirstOrDefault(g => g.Id == emp.GroupId);
            if (group == null) continue;
            var codes = _repo.GetMonthCodes(emp.Id, prevYear, prevMonth);
            var carryIn = _repo.GetCarryIn(emp.Id, prevYear, prevMonth);
            var summary = ScheduleCalculator.SummarizeMonth(
                group.Type, group.DailyHours, group.ShiftTypes, emp.EmploymentFactor, baseHours,
                codes, holidayMap, carryIn, prevYear, prevMonth);
            _repo.SetCarryIn(emp.Id, _selYear, _selMonth, Math.Round(summary.Balance, 2));
        }

        RebuildGrid();
        MessageBox.Show(Window.GetWindow(this),
            $"Előző havi ({AppDatabaseService.MonthNames[prevMonth - 1]} {prevYear}) egyenleg átmásolva bejövő óraként.",
            "Beosztás Varázsló", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void RebuildCoverage(List<WorkGroup> groups, List<Employee> employees)
    {
        CoveragePanel.Children.Clear();
        var relevant = groups.Where(g => g.StaffPerShift > 0 && g.ShiftTypes.Count > 0).ToList();
        if (relevant.Count == 0)
        {
            CoveragePanel.Children.Add(new TextBlock
            {
                Text = "Nincs olyan munkacsoport, amelyhez létszám-elvárás lenne beállítva (Munkacsoportok fül).",
                Foreground = (Brush)FindResource("TextSecondaryBrush"),
                FontSize = 12
            });
            return;
        }

        var dim = ScheduleCalculator.DaysInMonth(_selYear, _selMonth);
        var codesForAll = _repo.GetMonthCodesForAll(_selYear, _selMonth);

        foreach (var group in relevant)
        {
            CoveragePanel.Children.Add(new TextBlock
            {
                Text = $"{group.Name} (elvárt létszám/műszak: {group.StaffPerShift} fő)",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 10, 0, 6),
                Foreground = (Brush)FindResource("TextPrimaryBrush")
            });

            var groupEmployeeIds = employees.Where(e => e.GroupId == group.Id).Select(e => e.Id).ToList();

            var outer = new Grid();
            outer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            outer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var leftCol = new StackPanel();
            var rightColScroll = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled };
            var rightCol = new StackPanel();
            rightColScroll.Content = rightCol;
            Grid.SetColumn(leftCol, 0);
            Grid.SetColumn(rightColScroll, 1);

            leftCol.Children.Add(HeaderCellBorder("Műszak", NameColWidth, leftAlign: true));
            var headerRow = new StackPanel { Orientation = Orientation.Horizontal };
            for (int d = 1; d <= dim; d++) headerRow.Children.Add(HeaderCellBorder(d.ToString(), DayColWidth));
            rightCol.Children.Add(headerRow);

            foreach (var st in group.ShiftTypes)
            {
                leftCol.Children.Add(new Border
                {
                    Width = NameColWidth, Height = RowHeight,
                    Child = new TextBlock
                    {
                        Text = $"{st.Label} ({st.Code})", VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(8, 0, 0, 0), Foreground = (Brush)FindResource("TextPrimaryBrush")
                    }
                });
                var row = new StackPanel { Orientation = Orientation.Horizontal };
                for (int d = 1; d <= dim; d++)
                {
                    var codesThatDay = groupEmployeeIds.Select(id => codesForAll.TryGetValue(id, out var days) && days.TryGetValue(d, out var c) ? c : null);
                    var counts = ScheduleCalculator.ShiftCoverage(group.ShiftTypes, codesThatDay);
                    var n = counts.GetValueOrDefault(st.Code, 0);
                    var colorRes = n < group.StaffPerShift ? "DangerBrush" : n > group.StaffPerShift ? "WarningBrush" : "SuccessBrush";
                    row.Children.Add(CellBorder(n.ToString(), DayColWidth, bold: true, foreground: (Brush)FindResource(colorRes)));
                }
                rightCol.Children.Add(row);
            }

            outer.Children.Add(leftCol);
            outer.Children.Add(rightColScroll);
            CoveragePanel.Children.Add(outer);
        }
    }
}
