using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BeosztasVarazslo.Models;
using BeosztasVarazslo.Services;

namespace BeosztasVarazslo.Views;

public partial class GroupsView : UserControl, IRefreshableView
{
    private readonly AppRepository _repo;

    public GroupsView(AppRepository repo)
    {
        InitializeComponent();
        _repo = repo;
        Refresh();
    }

    public void Refresh()
    {
        GroupsPanel.Children.Clear();
        foreach (var group in _repo.GetGroupsSorted())
        {
            GroupsPanel.Children.Add(BuildGroupCard(group));
        }
    }

    private Border BuildGroupCard(WorkGroup group)
    {
        var empCount = _repo.EmployeeCountForGroup(group.Id);
        var typeLabel = group.Type == GroupTypes.Office ? "Iroda (hétfő-péntek)" : "Általános (váltásos/napi)";
        var shiftDesc = string.Join(", ", group.ShiftTypes.Select(s => $"{s.Label} ({s.Code}, {s.Hours} óra)"));

        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = $"{group.Name}  •  {typeLabel}",
            FontWeight = FontWeights.SemiBold,
            FontSize = 15,
            Foreground = (Brush)FindResource("TextPrimaryBrush")
        });
        stack.Children.Add(new TextBlock
        {
            Text = $"Napi óraszám: {group.DailyHours} óra · Műszakok: {(string.IsNullOrEmpty(shiftDesc) ? "—" : shiftDesc)}" +
                   (group.StaffPerShift > 0 ? $" · Létszám/műszak: {group.StaffPerShift} fő" : "") +
                   $" · Dolgozók: {empCount} fő",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 10),
            Foreground = (Brush)FindResource("TextSecondaryBrush"),
            FontSize = 12
        });

        var buttonRow = new StackPanel { Orientation = Orientation.Horizontal };
        var editBtn = new Button { Content = "Szerkesztés", Style = (Style)FindResource("LinkButton") };
        editBtn.Click += (_, _) => OpenEditWindow(group);
        var deleteBtn = new Button { Content = "Törlés", Style = (Style)FindResource("DangerButton") };
        deleteBtn.Click += (_, _) => DeleteGroup(group, empCount);
        buttonRow.Children.Add(editBtn);
        buttonRow.Children.Add(deleteBtn);
        stack.Children.Add(buttonRow);

        return new Border { Style = (Style)FindResource("CardBorder"), Child = stack };
    }

    private void DeleteGroup(WorkGroup group, int empCount)
    {
        if (empCount > 0)
        {
            MessageBox.Show(Window.GetWindow(this), "Nem törölhető: vannak hozzá rendelt dolgozók.",
                "Beosztás Varázsló", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            return;
        }
        var result = MessageBox.Show(Window.GetWindow(this), $"Biztosan törlöd a(z) \"{group.Name}\" munkacsoportot?",
            "Megerősítés", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;
        _repo.DeleteGroup(group);
        Refresh();
    }

    private void BtnAddGroup_Click(object sender, RoutedEventArgs e) => OpenEditWindow(null);

    private void OpenEditWindow(WorkGroup? existing)
    {
        var dlg = new GroupEditWindow(existing) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true && dlg.Result != null)
        {
            _repo.SaveGroup(dlg.Result);
            Refresh();
        }
    }
}
