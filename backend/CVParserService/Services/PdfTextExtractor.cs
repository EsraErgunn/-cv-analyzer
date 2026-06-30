using UglyToad.PdfPig;

namespace CVParserService.Services;

public class PdfTextExtractor
{
    public string ExtractText(Stream pdfStream)
    {
        using var document = PdfDocument.Open(pdfStream);
        var text = string.Empty;

        foreach (var page in document.GetPages())
        {
            text += page.Text + "\n";
        }

        return text;
    }
}