using System.Windows;
using BeosztasVarazslo.Models;

namespace BeosztasVarazslo.Views;

public partial class EmployeeEditWindow : Window
{
    private readonly Employee? _existing;
    private readonly List<WorkGroup> _groups;

    public Employee? Result { get; private set; }

    public EmployeeEditWindow(Employee? existing, List<WorkGroup> groups)
    {
        InitializeComponent();
        _existing = existing;
        _groups = groups;
        Title = existing == null ? "Új dolgozó" : "Dolgozó szerkesztése";

        CmbGroup.ItemsSource = _groups;

        if (existing != null)
        {
            TxtName.Text = existing.Name;
            var idx = _groups.FindIndex(g => g.Id == existing.GroupId);
            CmbGroup.SelectedIndex = idx >= 0 ? idx : 0;
            TxtFactor.Text = FormatNum(existing.EmploymentFactor);
            TxtMaxVacation.Text = existing.MaxVacationDays.ToString();
            TxtNotes.Text = existing.Notes;
        }
        else
        {
            CmbGroup.SelectedIndex = 0;
            TxtFactor.Text = "1";
            TxtMaxVacation.Text = "20";
        }
    }

    private static string FormatNum(double d) => d == Math.Floor(d) ? d.ToString("0") : d.ToString("0.##");

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        var name = TxtName.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            MessageBox.Show(this, "A név megadása kötelező.", "Beosztás Varázsló", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            return;
        }
        if (CmbGroup.SelectedItem is not WorkGroup selectedGroup)
        {
            MessageBox.Show(this, "Válassz munkacsoportot.", "Beosztás Varázsló", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            return;
        }
        if (!int.TryParse(TxtMaxVacation.Text, out var maxVac) || maxVac < 0)
        {
            MessageBox.Show(this, "A max. kiadható szabadság megadása kötelező (0 vagy több).", "Beosztás Varázsló",
                MessageBoxButton.OK, MessageBoxImage.Exclamation);
            return;
        }
        var factor = double.TryParse(TxtFactor.Text, out var f) && f > 0 ? f : 1.0;

        Result = new Employee
        {
            Id = _existing?.Id ?? 0,
            Name = name,
            GroupId = selectedGroup.Id,
            EmploymentFactor = factor,
            MaxVacationDays = maxVac,
            Notes = TxtNotes.Text.Trim()
        };
        DialogResult = true;
        Close();
    }
}
