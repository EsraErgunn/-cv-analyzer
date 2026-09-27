using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace CVParserService.Services;

public class PdfTextExtractor
{
    public string ExtractText(Stream pdfStream)
    {
        using var document = PdfDocument.Open(pdfStream);
        var text = new StringBuilder();

        foreach (var page in document.GetPages())
        {
            // page.Text satır sonlarını kaybeder; bölüm başlıklarını bulabilmek için
            // satır yapısını koruyan ContentOrderTextExtractor kullanılır.
            text.AppendLine(ContentOrderTextExtractor.GetText(page));
        }

        return text.ToString();
    }
}
