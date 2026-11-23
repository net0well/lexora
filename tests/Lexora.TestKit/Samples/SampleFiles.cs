namespace Lexora.TestKit.Samples;

public static class SampleFiles
{
    public const string SafetyManualPdf = "manual-seguranca.pdf";
    public const string VacationPolicyDocx = "politica-ferias.docx";
    public const string QuoteXlsx = "orcamento-epis.xlsx";
    public const string FaqMarkdown = "perguntas-frequentes.md";
    public const string MeetingNotesTxt = "notas-reuniao.txt";
    public const string ContactsCsv = "contatos.csv";
    public const string ContactsWindows1252Csv = "contatos-windows-1252.csv";

    public static string PathOf(string name) => Path.Combine(AppContext.BaseDirectory, "Samples", "Files", name);

    public static Stream Open(string name) => File.OpenRead(PathOf(name));
}
