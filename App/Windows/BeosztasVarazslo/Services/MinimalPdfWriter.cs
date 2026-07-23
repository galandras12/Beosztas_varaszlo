using System.Text;

namespace BeosztasVarazslo.Services;

/// <summary>
/// Minimális, saját kezűleg írt PDF-generátor: minden oldal egyetlen, teljes oldalt kitöltő
/// JPEG képként kerül be (DCTDecode szűrő), így a PDF szintaxis kizárólag ASCII karaktereket
/// tartalmaz - a tényleges (ékezetes) szöveg a WPF által renderelt JPEG képen belül van,
/// ezért nincs szükség PDF betűkészlet-kódolási trükkökre. Külső könyvtárat nem igényel.
/// </summary>
public static class MinimalPdfWriter
{
    public record PdfPageImage(byte[] JpegBytes, int PixelWidth, int PixelHeight, double PageWidthPt, double PageHeightPt);

    public static byte[] Build(List<PdfPageImage> pages)
    {
        using var ms = new MemoryStream();
        var offsets = new List<long> { 0 }; // 0. index nem használt (az objektumszámok 1-től indulnak)

        void WriteAscii(string s)
        {
            var bytes = Encoding.ASCII.GetBytes(s);
            ms.Write(bytes, 0, bytes.Length);
        }

        void RecordOffset(int objNum)
        {
            while (offsets.Count <= objNum) offsets.Add(0);
            offsets[objNum] = ms.Position;
        }

        // %PDF fejléc + bináris jelzés (szokásos gyakorlat, nem kötelező, de javítja a kompatibilitást)
        WriteAscii("%PDF-1.4\n");
        ms.Write(new byte[] { 0x25, 0xE2, 0xE3, 0xCF, 0xD3, 0x0A }, 0, 6);

        int pageCount = pages.Count;
        int totalObjects = 2 + pageCount * 3; // 1=Catalog, 2=Pages, majd (Page,Content,Image) oldalanként

        // 1. Catalog
        RecordOffset(1);
        WriteAscii("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

        // 2. Pages (Kids lista)
        var kids = new StringBuilder();
        for (int i = 0; i < pageCount; i++)
        {
            int pageObjNum = 3 + i * 3;
            kids.Append(pageObjNum).Append(" 0 R ");
        }
        RecordOffset(2);
        WriteAscii($"2 0 obj\n<< /Type /Pages /Kids [{kids.ToString().Trim()}] /Count {pageCount} >>\nendobj\n");

        for (int i = 0; i < pageCount; i++)
        {
            var page = pages[i];
            int pageObjNum = 3 + i * 3;
            int contentObjNum = 4 + i * 3;
            int imageObjNum = 5 + i * 3;

            // Page objektum
            RecordOffset(pageObjNum);
            WriteAscii(
                $"{pageObjNum} 0 obj\n" +
                $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {Fmt(page.PageWidthPt)} {Fmt(page.PageHeightPt)}] " +
                $"/Resources << /XObject << /Im0 {imageObjNum} 0 R >> >> /Contents {contentObjNum} 0 R >>\n" +
                "endobj\n");

            // Content stream: a teljes oldalra méretezett kép kirajzolása
            var content = $"q {Fmt(page.PageWidthPt)} 0 0 {Fmt(page.PageHeightPt)} 0 0 cm /Im0 Do Q";
            var contentBytes = Encoding.ASCII.GetBytes(content);
            RecordOffset(contentObjNum);
            WriteAscii($"{contentObjNum} 0 obj\n<< /Length {contentBytes.Length} >>\nstream\n");
            ms.Write(contentBytes, 0, contentBytes.Length);
            WriteAscii("\nendstream\nendobj\n");

            // Image XObject (nyers JPEG bájtok, DCTDecode)
            RecordOffset(imageObjNum);
            WriteAscii(
                $"{imageObjNum} 0 obj\n" +
                $"<< /Type /XObject /Subtype /Image /Width {page.PixelWidth} /Height {page.PixelHeight} " +
                $"/ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length {page.JpegBytes.Length} >>\nstream\n");
            ms.Write(page.JpegBytes, 0, page.JpegBytes.Length);
            WriteAscii("\nendstream\nendobj\n");
        }

        long xrefOffset = ms.Position;
        WriteAscii($"xref\n0 {totalObjects + 1}\n");
        WriteAscii("0000000000 65535 f \n");
        for (int obj = 1; obj <= totalObjects; obj++)
        {
            WriteAscii($"{offsets[obj]:D10} 00000 n \n");
        }
        WriteAscii($"trailer\n<< /Size {totalObjects + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");

        return ms.ToArray();
    }

    private static string Fmt(double v) => v.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
}
