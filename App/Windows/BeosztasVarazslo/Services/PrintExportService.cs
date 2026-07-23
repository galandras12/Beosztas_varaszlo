using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BeosztasVarazslo.Services;

/// <summary>
/// A4 fekvő elrendezésű nyomtatható táblázat előállítása. A tartalmat WPF elemekből (Grid/Border/
/// TextBlock) építi fel, majd RenderTargetBitmap + JpegBitmapEncoder segítségével JPEG-be rasztereli
/// (ezért a magyar ékezetes karakterek a rendszer valós betűkészletével, helyesen jelennek meg).
/// A PDF export ezekből a JPEG "oldalképekből" épül fel a <see cref="MinimalPdfWriter"/> segítségével -
/// így sem a PDF-generáláshoz, sem a képek elkészítéséhez nincs szükség külső NuGet-csomagra.
/// </summary>
public static class PrintExportService
{
    // A4 fekvő tervezési méret 96 DPI-s (WPF) egységekben.
    private const double PageWidthPx = 1123;
    private const double PageHeightPx = 794;
    private const double PageWidthPt = 842;
    private const double PageHeightPt = 595;

    private const double Margin = 20;
    private const double TitleHeight = 26;
    private const double HeaderRowHeight = 30;
    private const double PdfRowHeight = 18;
    private const double NameColWidth = 150;
    private const double BalanceColWidth = 68;
    private const double RenderDpi = 200;

    private static readonly Brush BlackBrush = Brushes.Black;
    private static readonly Brush GridLineBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60));
    private static readonly Brush HeaderBg = new SolidColorBrush(Color.FromRgb(224, 224, 224));
    private static readonly Brush GroupBg = new SolidColorBrush(Color.FromRgb(208, 208, 208));

    public static void ExportPdf(PrintDocument doc, string filePath)
    {
        double usableWidth = PageWidthPx - 2 * Margin;
        int dayCount = Math.Max(doc.DayHeaderLabels.Count, 1);
        double dayColWidth = (usableWidth - NameColWidth - BalanceColWidth) / dayCount;
        double usableHeight = PageHeightPx - 2 * Margin - TitleHeight - HeaderRowHeight;
        int rowsPerPage = Math.Max((int)(usableHeight / PdfRowHeight), 1);
        int totalPages = Math.Max(1, (int)Math.Ceiling(doc.Rows.Count / (double)rowsPerPage));

        var pages = new List<MinimalPdfWriter.PdfPageImage>();
        int rowIdx = 0, pageNum = 1;

        while (rowIdx < doc.Rows.Count || pageNum == 1)
        {
            int endIdx = Math.Min(rowIdx + rowsPerPage, doc.Rows.Count);
            var pageRows = doc.Rows.GetRange(rowIdx, endIdx - rowIdx);

            var visual = BuildPageVisual(doc, pageRows, pageNum, totalPages, dayColWidth);
            var jpeg = RenderToJpeg(visual, PageWidthPx, PageHeightPx, RenderDpi, out int pxW, out int pxH);
            pages.Add(new MinimalPdfWriter.PdfPageImage(jpeg, pxW, pxH, PageWidthPt, PageHeightPt));

            rowIdx = endIdx;
            if (rowIdx >= doc.Rows.Count) break;
            pageNum++;
        }

        File.WriteAllBytes(filePath, MinimalPdfWriter.Build(pages));
    }

    public static void ExportJpg(PrintDocument doc, string filePath)
    {
        const double dayColWidthCont = 34;
        const double rowHeightCont = 26;
        int dayCount = Math.Max(doc.DayHeaderLabels.Count, 1);

        double width = 2 * Margin + NameColWidth + dayCount * dayColWidthCont + BalanceColWidth;
        double height = 2 * Margin + TitleHeight + HeaderRowHeight + doc.Rows.Count * rowHeightCont;

        var content = BuildPageContent(doc, doc.Rows, 1, 1, dayColWidthCont, rowHeightCont, isSinglePage: true);
        var root = new Border { Width = width, Height = height, Background = Brushes.White, Child = content };

        var jpeg = RenderToJpeg(root, width, height, 150, out _, out _);
        File.WriteAllBytes(filePath, jpeg);
    }

    private static FrameworkElement BuildPageVisual(PrintDocument doc, List<PrintRow> pageRows, int pageNum, int totalPages, double dayColWidth)
    {
        var content = BuildPageContent(doc, pageRows, pageNum, totalPages, dayColWidth, PdfRowHeight, isSinglePage: false);
        return new Border { Width = PageWidthPx, Height = PageHeightPx, Background = Brushes.White, Child = content };
    }

    private static FrameworkElement BuildPageContent(
        PrintDocument doc, List<PrintRow> rows, int pageNum, int totalPages, double dayColWidth, double rowHeight, bool isSinglePage)
    {
        var stack = new StackPanel { Margin = new Thickness(Margin) };
        var titleText = isSinglePage ? doc.Title : $"{doc.Title}  –  {pageNum}. / {totalPages}. oldal";
        stack.Children.Add(new TextBlock
        {
            Text = titleText, FontSize = 15, FontWeight = FontWeights.Bold,
            Foreground = BlackBrush, Margin = new Thickness(0, 0, 0, 8)
        });

        stack.Children.Add(BuildRow(null, true, doc.DayHeaderLabels, "", dayColWidth, rowHeight));

        foreach (var row in rows)
        {
            if (row.IsGroupHeader)
                stack.Children.Add(BuildGroupHeaderRow(row.Label, dayColWidth, doc.DayHeaderLabels.Count, rowHeight));
            else
                stack.Children.Add(BuildRow(row.Label, false, row.DayTexts, row.BalanceText, dayColWidth, rowHeight));
        }

        return stack;
    }

    private static Border DataCell(string text, double width, double height, bool bold = false, Brush? background = null)
    {
        return new Border
        {
            Width = width,
            Height = height,
            Background = background ?? Brushes.White,
            BorderBrush = GridLineBrush,
            BorderThickness = new Thickness(0.5),
            Child = new TextBlock
            {
                Text = text,
                FontSize = 9,
                FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
                Foreground = BlackBrush,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.NoWrap
            }
        };
    }

    private static StackPanel BuildRow(string? name, bool isHeader, List<string> dayTexts, string balanceText, double dayColWidth, double rowHeight)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        row.Children.Add(DataCell(name ?? "Dolgozó (munkakör szerint, ABC sorrendben)", NameColWidth, rowHeight, isHeader, isHeader ? HeaderBg : null));
        foreach (var text in dayTexts)
            row.Children.Add(DataCell(text, dayColWidth, rowHeight, isHeader, isHeader ? HeaderBg : null));
        row.Children.Add(DataCell(isHeader ? "Köv. hóra átvitt óra" : balanceText, BalanceColWidth, rowHeight, true, isHeader ? HeaderBg : null));
        return row;
    }

    private static StackPanel BuildGroupHeaderRow(string label, double dayColWidth, int dayCount, double rowHeight)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        double fullWidth = NameColWidth + dayCount * dayColWidth + BalanceColWidth;
        row.Children.Add(new Border
        {
            Width = fullWidth,
            Height = rowHeight,
            Background = GroupBg,
            BorderBrush = GridLineBrush,
            BorderThickness = new Thickness(0.5),
            Child = new TextBlock
            {
                Text = label, FontSize = 10, FontWeight = FontWeights.Bold, Foreground = BlackBrush,
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 0, 0)
            }
        });
        return row;
    }

    private static byte[] RenderToJpeg(FrameworkElement visual, double designWidth, double designHeight, double dpi, out int pxW, out int pxH)
    {
        visual.Measure(new Size(designWidth, designHeight));
        visual.Arrange(new Rect(0, 0, designWidth, designHeight));
        visual.UpdateLayout();

        pxW = (int)Math.Round(designWidth * dpi / 96.0);
        pxH = (int)Math.Round(designHeight * dpi / 96.0);

        var rtb = new RenderTargetBitmap(pxW, pxH, dpi, dpi, PixelFormats.Pbgra32);
        rtb.Render(visual);

        var encoder = new JpegBitmapEncoder { QualityLevel = 92 };
        encoder.Frames.Add(BitmapFrame.Create(rtb));
        using var ms = new MemoryStream();
        encoder.Save(ms);
        return ms.ToArray();
    }
}
