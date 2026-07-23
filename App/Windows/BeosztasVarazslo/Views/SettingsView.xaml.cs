using System.Windows;
using System.Windows.Controls;
using BeosztasVarazslo.Services;

namespace BeosztasVarazslo.Views;

public partial class SettingsView : UserControl, IRefreshableView
{
    private readonly AppRepository _repo;
    private readonly List<(TextBox Hours, TextBox Days)> _monthRows = new();
    private bool _suppressEvents;

    public SettingsView(AppRepository repo)
    {
        InitializeComponent();
        _repo = repo;
        TxtHolidayYear.Text = DateTime.Now.Year.ToString();
        BuildMonthHoursRows();
        Refresh();
    }

    public void Refresh()
    {
        LoadMonthHours();
        LoadHolidays();
    }

    private void BuildMonthHoursRows()
    {
        MonthHoursPanel.Children.Clear();
        _monthRows.Clear();

        for (int m = 1; m <= 12; m++)
        {
            var row = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var nameText = new TextBlock
            {
                Text = AppDatabaseService.MonthNames[m - 1],
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (System.Windows.Media.Brush)FindResource("TextPrimaryBrush")
            };
            Grid.SetColumn(nameText, 0);

            var hoursBox = new TextBox { Margin = new Thickness(6, 0, 6, 0) };
            Grid.SetColumn(hoursBox, 1);
            int month = m;
            hoursBox.LostFocus += (_, _) => SaveMonthRow(month);

            var daysBox = new TextBox { Margin = new Thickness(6, 0, 0, 0) };
            Grid.SetColumn(daysBox, 2);
            daysBox.LostFocus += (_, _) => SaveMonthRow(month);

            row.Children.Add(nameText);
            row.Children.Add(hoursBox);
            row.Children.Add(daysBox);
            MonthHoursPanel.Children.Add(row);
            _monthRows.Add((hoursBox, daysBox));
        }
    }

    private void SaveMonthRow(int month)
    {
        if (_suppressEvents) return;
        var (hoursBox, daysBox) = _monthRows[month - 1];
        if (double.TryParse(hoursBox.Text, out var hours) && int.TryParse(daysBox.Text, out var days))
        {
            _repo.SetMonthHours(month, hours, days);
        }
    }

    private void LoadMonthHours()
    {
        _suppressEvents = true;
        for (int m = 1; m <= 12; m++)
        {
            var rec = _repo.GetMonthHours(m);
            if (rec == null) continue;
            var (hoursBox, daysBox) = _monthRows[m - 1];
            hoursBox.Text = rec.Hours == Math.Floor(rec.Hours) ? rec.Hours.ToString("0") : rec.Hours.ToString("0.##");
            daysBox.Text = rec.Days.ToString();
        }
        _suppressEvents = false;
    }

    private void BtnResetMonthHours_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(Window.GetWindow(this),
            "Visszaállítod az alapértelmezett havi óraszám-táblát?", "Megerősítés",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;
        _repo.ResetMonthHoursToDefault();
        LoadMonthHours();
    }

    private int CurrentHolidayYear() =>
        int.TryParse(TxtHolidayYear.Text, out var y) ? y : DateTime.Now.Year;

    private void TxtHolidayYear_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (int.TryParse(TxtHolidayYear.Text, out _)) LoadHolidays();
    }

    private void LoadHolidays()
    {
        HolidayListPanel.Children.Clear();
        var year = CurrentHolidayYear();
        var auto = HungarianHolidays.DefaultHolidays(year);
        var removed = _repo.GetRemovedHolidays(year);
        var extra = _repo.GetExtraHolidays(year);

        var dates = auto.Keys.Concat(extra.Keys).Distinct().OrderBy(d => d).ToList();

        foreach (var date in dates)
        {
            bool isAuto = auto.ContainsKey(date);
            bool isRemoved = isAuto && removed.Contains(date);
            string name = isAuto ? auto[date] : extra[date];

            var row = new Grid { Margin = new Thickness(0, 3, 0, 3), Opacity = isRemoved ? 0.45 : 1.0 };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(95) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });

            var dateText = new TextBlock { Text = date, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(dateText, 0);

            var nameText = new TextBlock { Text = name, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
            Grid.SetColumn(nameText, 1);

            var typeText = new TextBlock
            {
                Text = isAuto ? "automatikus" : "egyéni",
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (System.Windows.Media.Brush)FindResource("TextSecondaryBrush"),
                FontSize = 11
            };
            Grid.SetColumn(typeText, 2);

            var actionBtn = new Button { Style = (Style)FindResource("LinkButton"), Padding = new Thickness(6, 2, 6, 2) };
            Grid.SetColumn(actionBtn, 3);
            if (isAuto)
            {
                actionBtn.Content = isRemoved ? "Visszaállítás" : "Kikapcsolás";
                actionBtn.Click += (_, _) =>
                {
                    _repo.SetHolidayRemoved(year, date, !isRemoved);
                    LoadHolidays();
                };
            }
            else
            {
                actionBtn.Content = "Törlés";
                actionBtn.Foreground = (System.Windows.Media.Brush)FindResource("DangerBrush");
                actionBtn.Click += (_, _) =>
                {
                    _repo.DeleteExtraHoliday(year, date);
                    LoadHolidays();
                };
            }

            row.Children.Add(dateText);
            row.Children.Add(nameText);
            row.Children.Add(typeText);
            row.Children.Add(actionBtn);
            HolidayListPanel.Children.Add(row);
        }
    }

    private void BtnAddHoliday_Click(object sender, RoutedEventArgs e)
    {
        var date = DpNewHolidayDate.SelectedDate;
        var name = TxtNewHolidayName.Text.Trim();
        if (date == null || string.IsNullOrEmpty(name))
        {
            MessageBox.Show(Window.GetWindow(this), "Add meg a dátumot és a nevet is.", "Beosztás Varázsló",
                MessageBoxButton.OK, MessageBoxImage.Exclamation);
            return;
        }
        var year = date.Value.Year;
        var iso = date.Value.ToString("yyyy-MM-dd");
        _repo.AddExtraHoliday(year, iso, name);
        TxtNewHolidayName.Clear();
        DpNewHolidayDate.SelectedDate = null;
        TxtHolidayYear.Text = year.ToString();
        LoadHolidays();
    }
}
