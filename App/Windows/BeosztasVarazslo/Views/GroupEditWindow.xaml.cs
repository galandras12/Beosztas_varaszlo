using System.Windows;
using System.Windows.Controls;
using BeosztasVarazslo.Models;

namespace BeosztasVarazslo.Views;

public partial class GroupEditWindow : Window
{
    private readonly WorkGroup? _existing;
    private readonly List<ShiftTypeRow> _shiftRows = new();

    public WorkGroup? Result { get; private set; }

    private class ShiftTypeRow
    {
        public required Grid Row { get; init; }
        public required TextBox Code { get; init; }
        public required TextBox Label { get; init; }
        public required TextBox Hours { get; init; }
    }

    public GroupEditWindow(WorkGroup? existing)
    {
        InitializeComponent();
        _existing = existing;
        Title = existing == null ? "Új munkacsoport" : "Munkacsoport szerkesztése";
        Populate();
    }

    private void Populate()
    {
        if (_existing != null)
        {
            TxtName.Text = _existing.Name;
            RbOffice.IsChecked = _existing.Type == GroupTypes.Office;
            RbGeneral.IsChecked = _existing.Type != GroupTypes.Office;
            TxtDailyHours.Text = FormatNum(_existing.DailyHours);
            TxtStaffPerShift.Text = _existing.StaffPerShift.ToString();
            TxtMinRestHours.Text = _existing.MinRestHours.ToString();

            if (_existing.ShiftTypes.Count == 0)
                AddShiftRow("M", "Munka", FormatNum(_existing.DailyHours));
            else
                foreach (var st in _existing.ShiftTypes) AddShiftRow(st.Code, st.Label, FormatNum(st.Hours));
        }
        else
        {
            TxtDailyHours.Text = "8";
            TxtStaffPerShift.Text = "0";
            TxtMinRestHours.Text = "24";
            AddShiftRow("M", "Munka", "8");
        }
        SyncForType();
    }

    private static string FormatNum(double d) => d == Math.Floor(d) ? d.ToString("0") : d.ToString("0.##");

    private void AddShiftRow(string code, string label, string hours)
    {
        var row = new Grid { Margin = new Thickness(0, 3, 0, 3) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var codeBox = new TextBox { Text = code, Margin = new Thickness(0, 0, 4, 0) };
        Grid.SetColumn(codeBox, 0);
        var labelBox = new TextBox { Text = label, Margin = new Thickness(0, 0, 4, 0) };
        Grid.SetColumn(labelBox, 1);
        var hoursBox = new TextBox { Text = hours, Margin = new Thickness(0, 0, 4, 0) };
        Grid.SetColumn(hoursBox, 2);
        var removeBtn = new Button { Content = "✕", Style = (Style)FindResource("LinkButton"), Padding = new Thickness(8, 4, 8, 4) };
        Grid.SetColumn(removeBtn, 3);

        row.Children.Add(codeBox);
        row.Children.Add(labelBox);
        row.Children.Add(hoursBox);
        row.Children.Add(removeBtn);

        var entry = new ShiftTypeRow { Row = row, Code = codeBox, Label = labelBox, Hours = hoursBox };
        removeBtn.Click += (_, _) =>
        {
            ShiftTypesPanel.Children.Remove(row);
            _shiftRows.Remove(entry);
        };

        ShiftTypesPanel.Children.Add(row);
        _shiftRows.Add(entry);
    }

    private void Type_Changed(object sender, RoutedEventArgs e) => SyncForType();

    private void TxtDailyHours_LostFocus(object sender, RoutedEventArgs e) => SyncForType();

    private void SyncForType()
    {
        bool isOffice = RbOffice.IsChecked == true;
        BtnAddShiftType.Visibility = isOffice ? Visibility.Collapsed : Visibility.Visible;
        TxtMinRestLabel.Visibility = isOffice ? Visibility.Collapsed : Visibility.Visible;
        TxtMinRestHours.Visibility = isOffice ? Visibility.Collapsed : Visibility.Visible;

        if (isOffice)
        {
            ShiftTypesPanel.Children.Clear();
            _shiftRows.Clear();
            var dh = double.TryParse(TxtDailyHours.Text, out var v) ? v : 8;
            AddShiftRow("M", "Munka", FormatNum(dh));
        }
    }

    private void BtnAddShiftType_Click(object sender, RoutedEventArgs e) => AddShiftRow("", "", "");

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
            MessageBox.Show(this, "A munkacsoport neve kötelező.", "Beosztás Varázsló", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            return;
        }
        var type = RbOffice.IsChecked == true ? GroupTypes.Office : GroupTypes.General;
        var dailyHours = double.TryParse(TxtDailyHours.Text, out var dh) ? dh : 0;
        var staffPerShift = int.TryParse(TxtStaffPerShift.Text, out var sps) ? sps : 0;
        var minRestHours = int.TryParse(TxtMinRestHours.Text, out var mrh) && mrh >= 0 ? mrh : 24;

        var shiftTypes = new List<ShiftType>();
        foreach (var row in _shiftRows)
        {
            var code = row.Code.Text.Trim();
            if (string.IsNullOrEmpty(code)) continue;
            shiftTypes.Add(new ShiftType
            {
                Code = code,
                Label = row.Label.Text.Trim(),
                Hours = double.TryParse(row.Hours.Text, out var h) ? h : 0
            });
        }
        if (shiftTypes.Count == 0)
        {
            MessageBox.Show(this, "Legalább egy műszaktípus szükséges.", "Beosztás Varázsló", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            return;
        }

        Result = new WorkGroup
        {
            Id = _existing?.Id ?? 0,
            Name = name,
            Type = type,
            DailyHours = dailyHours,
            StaffPerShift = staffPerShift,
            MinRestHours = minRestHours,
            ShiftTypes = shiftTypes
        };
        DialogResult = true;
        Close();
    }
}
