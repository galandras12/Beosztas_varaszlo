using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using BeosztasVarazslo.Services;
using BeosztasVarazslo.Views;

namespace BeosztasVarazslo;

public partial class MainWindow : Window
{
    private readonly AppRepository _repo = new();

    private readonly SettingsView _settingsView;
    private readonly GroupsView _groupsView;
    private readonly EmployeesView _employeesView;
    private readonly ScheduleView _scheduleView;
    private readonly PrintView _printView;

    public MainWindow()
    {
        InitializeComponent();

        _settingsView = new SettingsView(_repo);
        _groupsView = new GroupsView(_repo);
        _employeesView = new EmployeesView(_repo);
        _scheduleView = new ScheduleView(_repo);
        _printView = new PrintView(_repo);

        NavList.SelectedIndex = 0;
    }

    private void NavList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NavList.SelectedItem is not ListBoxItem item) return;
        var tag = item.Tag as string;

        UserControl view = tag switch
        {
            "settings" => _settingsView,
            "groups" => _groupsView,
            "employees" => _employeesView,
            "schedule" => _scheduleView,
            "print" => _printView,
            _ => _settingsView
        };

        (view as IRefreshableView)?.Refresh();
        PageHost.Content = view;
    }

    private void BtnExportDb_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog
        {
            Filter = "JSON adatbázis (*.json)|*.json",
            FileName = $"beosztas-adatbazis-{DateTime.Now:yyyy-MM-dd_HHmm}.json"
        };
        if (dlg.ShowDialog(this) == true)
        {
            try
            {
                AppDatabaseService.ExportToFile(_repo.Db, dlg.FileName);
                MessageBox.Show(this, "Adatbázis fájlba mentve.", "Beosztás Varázsló", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Hiba a mentéskor: {ex.Message}", "Beosztás Varázsló", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void BtnImportDb_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(this,
            "A betöltés felülírja a jelenlegi adatokat. Folytatod?",
            "Megerősítés", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        var dlg = new OpenFileDialog { Filter = "JSON adatbázis (*.json)|*.json|Minden fájl (*.*)|*.*" };
        if (dlg.ShowDialog(this) == true)
        {
            try
            {
                var newDb = AppDatabaseService.ImportFromFile(dlg.FileName);
                _repo.ReplaceDatabase(newDb);
                RefreshAllViews();
                MessageBox.Show(this, "Adatbázis betöltve.", "Beosztás Varázsló", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Hiba a betöltéskor: {ex.Message}", "Beosztás Varázsló", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void BtnResetDb_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(this,
            "Ez törli az ÖSSZES adatot (munkacsoportok, dolgozók, beosztások) és visszaáll az alapértelmezett állapotra. Biztosan folytatod?",
            "Megerősítés", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        _repo.ResetAll();
        RefreshAllViews();
        MessageBox.Show(this, "Alaphelyzet visszaállítva.", "Beosztás Varázsló", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void RefreshAllViews()
    {
        _settingsView.Refresh();
        _groupsView.Refresh();
        _employeesView.Refresh();
        _scheduleView.Refresh();
        _printView.Refresh();
    }
}

/// <summary>Egy nézet, amely a fülre való visszalépéskor (vagy adatbázis-csere után) frissíti magát.</summary>
public interface IRefreshableView
{
    void Refresh();
}
