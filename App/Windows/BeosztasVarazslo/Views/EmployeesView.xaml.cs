using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BeosztasVarazslo.Models;
using BeosztasVarazslo.Services;

namespace BeosztasVarazslo.Views;

public partial class EmployeesView : UserControl, IRefreshableView
{
    private readonly AppRepository _repo;

    public EmployeesView(AppRepository repo)
    {
        InitializeComponent();
        _repo = repo;
        TxtVacationYear.Text = DateTime.Now.Year.ToString();
        Refresh();
    }

    public void Refresh() => Rebuild();

    private int CurrentYear() => int.TryParse(TxtVacationYear.Text, out var y) ? y : DateTime.Now.Year;

    private void TxtVacationYear_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (int.TryParse(TxtVacationYear.Text, out _)) Rebuild();
    }

    private void Rebuild()
    {
        EmployeesPanel.Children.Clear();
        var year = CurrentYear();
        var groups = _repo.GetGroupsSorted();

        if (groups.Count == 0)
        {
            EmployeesPanel.Children.Add(new TextBlock
            {
                Text = "Előbb hozz létre legalább egy munkacsoportot a Munkacsoportok fülön.",
                Foreground = (Brush)FindResource("TextSecondaryBrush")
            });
            return;
        }

        foreach (var group in groups)
        {
            var employees = _repo.GetEmployeesForGroup(group.Id);
            if (employees.Count == 0) continue;

            var card = new StackPanel();
            card.Children.Add(new TextBlock
            {
                Text = group.Name, FontWeight = FontWeights.SemiBold, FontSize = 15,
                Foreground = (Brush)FindResource("TextPrimaryBrush"), Margin = new Thickness(0, 0, 0, 8)
            });

            var header = BuildRow(new[] { "Név", "Munkaidő-arány", "Max. szabadság/év", $"Kivett ({year})", "Hátralévő", "" }, true);
            card.Children.Add(header);

            foreach (var emp in employees)
            {
                var used = _repo.YearVacationUsed(emp.Id, year);
                var remaining = emp.MaxVacationDays - used;

                var row = BuildRow(new[]
                {
                    emp.Name,
                    FormatNum(emp.EmploymentFactor),
                    emp.MaxVacationDays.ToString(),
                    used.ToString(),
                    remaining.ToString(),
                    ""
                }, false, remaining < 0);

                var actionPanel = new StackPanel { Orientation = Orientation.Horizontal };
                var editBtn = new Button { Content = "Szerkesztés", Style = (Style)FindResource("LinkButton") };
                editBtn.Click += (_, _) => OpenEditWindow(emp);
                var deleteBtn = new Button { Content = "Törlés", Style = (Style)FindResource("DangerButton") };
                deleteBtn.Click += (_, _) => DeleteEmployee(emp);
                actionPanel.Children.Add(editBtn);
                actionPanel.Children.Add(deleteBtn);
                row.Children.Add(actionPanel);
                Grid.SetColumn(actionPanel, 5);

                card.Children.Add(row);
            }

            EmployeesPanel.Children.Add(new Border { Style = (Style)FindResource("CardBorder"), Child = card });
        }
    }

    private Grid BuildRow(string[] cells, bool isHeader, bool warn = false)
    {
        var row = new Grid { Margin = new Thickness(0, isHeader ? 0 : 3, 0, isHeader ? 6 : 3) };
        var widths = new[] { 2.2, 1.3, 1.3, 1.1, 1.1, 1.6 };
        foreach (var w in widths) row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(w, GridUnitType.Star) });

        for (int i = 0; i < cells.Length && i < 5; i++)
        {
            var text = new TextBlock
            {
                Text = cells[i],
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = isHeader ? FontWeights.SemiBold : FontWeights.Normal,
                FontSize = isHeader ? 11 : 13,
                Foreground = isHeader
                    ? (Brush)FindResource("TextSecondaryBrush")
                    : (i == 4 && warn ? (Brush)FindResource("DangerBrush") : (Brush)FindResource("TextPrimaryBrush"))
            };
            Grid.SetColumn(text, i);
            row.Children.Add(text);
        }
        return row;
    }

    private static string FormatNum(double d) => d == Math.Floor(d) ? d.ToString("0") : d.ToString("0.##");

    private void DeleteEmployee(Employee emp)
    {
        var result = MessageBox.Show(Window.GetWindow(this),
            $"Biztosan törlöd \"{emp.Name}\" dolgozót? Ez a beosztási adatait is érvényteleníti.",
            "Megerősítés", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;
        _repo.DeleteEmployee(emp);
        Rebuild();
    }

    private void BtnAddEmployee_Click(object sender, RoutedEventArgs e) => OpenEditWindow(null);

    private void OpenEditWindow(Employee? existing)
    {
        var groups = _repo.GetGroupsSorted();
        if (groups.Count == 0)
        {
            MessageBox.Show(Window.GetWindow(this), "Előbb hozz létre legalább egy munkacsoportot a Munkacsoportok fülön.",
                "Beosztás Varázsló", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            return;
        }
        var dlg = new EmployeeEditWindow(existing, groups) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true && dlg.Result != null)
        {
            _repo.SaveEmployee(dlg.Result);
            Rebuild();
        }
    }
}
