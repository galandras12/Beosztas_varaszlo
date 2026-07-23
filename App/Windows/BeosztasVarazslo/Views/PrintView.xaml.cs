using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using BeosztasVarazslo.Services;

namespace BeosztasVarazslo.Views;

public partial class PrintView : UserControl, IRefreshableView
{
    private readonly AppRepository _repo;

    public PrintView(AppRepository repo)
    {
        InitializeComponent();
        _repo = repo;

        TxtYear.Text = DateTime.Now.Year.ToString();
        CmbMonth.ItemsSource = AppDatabaseService.MonthNames;
        CmbMonth.SelectedIndex = DateTime.Now.Month - 1;

        Refresh();
    }

    public void Refresh() => UpdateSummary();

    private int SelectedYear() => int.TryParse(TxtYear.Text, out var y) ? y : DateTime.Now.Year;
    private int SelectedMonth() => CmbMonth.SelectedIndex + 1;

    private void Selection_Changed(object sender, RoutedEventArgs e) => UpdateSummary();

    private void UpdateSummary()
    {
        var year = SelectedYear();
        var month = SelectedMonth();
        var groups = _repo.GetGroupsSorted();
        var employees = _repo.GetEmployeesSorted();
        var groupCount = groups.Count(g => employees.Any(e => e.GroupId == g.Id));
        var empCount = employees.Count(e => groups.Any(g => g.Id == e.GroupId));
        TxtSummary.Text = $"Exportálandó: {AppDatabaseService.MonthNames[month - 1]} {year} ({month}. hónap), " +
                           $"{empCount} dolgozó, {groupCount} munkacsoport.";
    }

    private static string ExportsDir()
    {
        var dir = Path.Combine(Path.GetDirectoryName(AppDatabaseService.GetDefaultDbPath())!, "exports");
        Directory.CreateDirectory(dir);
        return dir;
    }

    private void BtnExportPdf_Click(object sender, RoutedEventArgs e)
    {
        var doc = PrintDocumentBuilder.Build(_repo, SelectedYear(), SelectedMonth());
        var dlg = new SaveFileDialog
        {
            Filter = "PDF fájl (*.pdf)|*.pdf",
            InitialDirectory = ExportsDir(),
            FileName = $"beosztas-{AppDatabaseService.MonthNames[SelectedMonth() - 1]}-{SelectedYear()}.pdf"
        };
        if (dlg.ShowDialog(Window.GetWindow(this)) != true) return;

        try
        {
            PrintExportService.ExportPdf(doc, dlg.FileName);
            MessageBox.Show(Window.GetWindow(this), "PDF elmentve.", "Beosztás Varázsló", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(Window.GetWindow(this), $"Hiba a PDF létrehozásakor: {ex.Message}", "Beosztás Varázsló",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnExportJpg_Click(object sender, RoutedEventArgs e)
    {
        var doc = PrintDocumentBuilder.Build(_repo, SelectedYear(), SelectedMonth());
        var dlg = new SaveFileDialog
        {
            Filter = "JPG kép (*.jpg)|*.jpg",
            InitialDirectory = ExportsDir(),
            FileName = $"beosztas-{AppDatabaseService.MonthNames[SelectedMonth() - 1]}-{SelectedYear()}.jpg"
        };
        if (dlg.ShowDialog(Window.GetWindow(this)) != true) return;

        try
        {
            PrintExportService.ExportJpg(doc, dlg.FileName);
            MessageBox.Show(Window.GetWindow(this), "JPG elmentve.", "Beosztás Varázsló", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(Window.GetWindow(this), $"Hiba a JPG létrehozásakor: {ex.Message}", "Beosztás Varázsló",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo { FileName = ExportsDir(), UseShellExecute = true });
    }
}
