using CVParserService.Services;

namespace CvAnalyzer.Tests;

public class CvSectionParserTests
{
    private const string SampleCv = """
        Ayşe Yılmaz
        ayse@example.com
        Özet
        Backend geliştirici.
        Deneyim
        • Acme Yazılım - Backend Developer (2021-2024)
        • RabbitMQ ile mikroservis altyapısı kurdu
        Eğitim
        İstanbul Teknik Üniversitesi - Bilgisayar Mühendisliği
        Yetenekler:
        C#, Docker, PostgreSQL
        """;

    private readonly CvSectionParser _parser = new();

    [Fact]
    public void Parse_SplitsSectionsByTurkishHeadings()
    {
        var parsed = _parser.Parse(SampleCv);

        Assert.Equal(2, parsed.Experience.Count);
        Assert.Equal("Acme Yazılım - Backend Developer (2021-2024)", parsed.Experience[0]);
        Assert.Single(parsed.Education);
        Assert.StartsWith("İstanbul Teknik", parsed.Education[0]);
    }

    [Fact]
    public void Parse_ExtractsSkillsFromWholeText()
    {
        var parsed = _parser.Parse(SampleCv);

        Assert.Contains("C#", parsed.Skills);
        Assert.Contains("Docker", parsed.Skills);
        Assert.Contains("RabbitMQ", parsed.Skills); // Deneyim bölümünde geçiyor
        Assert.Contains("Microservices", parsed.Skills);
    }

    [Fact]
    public void Parse_EnglishHeadings()
    {
        var parsed = _parser.Parse("Experience\nSoftware Engineer at Contoso\nEducation\nMIT\n");

        Assert.Equal(new[] { "Software Engineer at Contoso" }, parsed.Experience);
        Assert.Equal(new[] { "MIT" }, parsed.Education);
    }
}
