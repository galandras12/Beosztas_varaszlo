using System.Windows;
using System.Windows.Controls;

namespace BeosztasVarazslo.Views;

public partial class CellPickerWindow : Window
{
    public string? SelectedCode { get; private set; }

    public CellPickerWindow(string headerText, List<(string Code, string Label)> options)
    {
        InitializeComponent();
        TxtHeader.Text = headerText;

        foreach (var (code, label) in options)
        {
            var btn = new Button
            {
                Content = label,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 2, 0, 2)
            };
            btn.Click += (_, _) =>
            {
                SelectedCode = code;
                DialogResult = true;
                Close();
            };
            OptionsPanel.Children.Add(btn);
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
