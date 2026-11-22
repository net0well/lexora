using System.Globalization;
using System.Text;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace Lexora.TestKit.Samples;

public static class SamplePdf
{
    public static byte[] WithPages(params string[][] pages)
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);

        foreach (var lines in pages)
        {
            var page = builder.AddPage(PageSize.A4);
            var top = 780.0;

            foreach (var line in lines)
            {
                page.AddText(line, 12, new PdfPoint(40, top), font);
                top -= 18;
            }
        }

        return builder.Build();
    }

    public static byte[] Blank(int pageCount)
    {
        var builder = new PdfDocumentBuilder();

        for (var page = 0; page < pageCount; page++)
        {
            builder.AddPage(PageSize.A4);
        }

        return builder.Build();
    }

    public static byte[] Encrypted()
    {
        string[] objects =
        [
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] >>",
            $"<< /Filter /Standard /V 1 /R 2 /O <{new string('A', 64)}> /U <{new string('B', 64)}> /P -4 >>",
        ];

        var pdf = new StringBuilder("%PDF-1.4\n");
        List<int> offsets = [];

        for (var index = 0; index < objects.Length; index++)
        {
            offsets.Add(pdf.Length);
            pdf.Append(CultureInfo.InvariantCulture, $"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
        }

        var xref = pdf.Length;
        pdf.Append(CultureInfo.InvariantCulture, $"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");

        foreach (var offset in offsets)
        {
            pdf.Append(CultureInfo.InvariantCulture, $"{offset:D10} 00000 n \n");
        }

        pdf.Append(CultureInfo.InvariantCulture, $"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R /Encrypt 4 0 R /ID [<{new string('C', 32)}> <{new string('C', 32)}>] >>\n");
        pdf.Append(CultureInfo.InvariantCulture, $"startxref\n{xref}\n%%EOF\n");

        return Encoding.ASCII.GetBytes(pdf.ToString());
    }
}
