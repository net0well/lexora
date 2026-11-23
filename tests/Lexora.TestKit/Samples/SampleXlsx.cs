using ClosedXML.Excel;

namespace Lexora.TestKit.Samples;

public static class SampleXlsx
{
    public static byte[] Create(params (string Name, string[][] Rows)[] sheets)
    {
        using var workbook = new XLWorkbook();

        foreach (var (name, rows) in sheets)
        {
            var sheet = workbook.AddWorksheet(name);

            for (var row = 0; row < rows.Length; row++)
            {
                for (var column = 0; column < rows[row].Length; column++)
                {
                    var value = rows[row][column];

                    if (value.StartsWith('='))
                    {
                        sheet.Cell(row + 1, column + 1).FormulaA1 = value[1..];
                    }
                    else if (value.Length > 0)
                    {
                        sheet.Cell(row + 1, column + 1).Value = value;
                    }
                }
            }
        }

        using var buffer = new MemoryStream();
        workbook.SaveAs(buffer, new SaveOptions { EvaluateFormulasBeforeSaving = true });

        return buffer.ToArray();
    }
}
