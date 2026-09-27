using AIAnalyzerService.Analyzers;
using Shared.Events;

namespace CvAnalyzer.Tests;

public class RuleBasedCvAnalyzerTests
{
    private readonly RuleBasedCvAnalyzer _analyzer = new();

    [Fact]
    public void Analyze_ComputesMatchedAndMissingSkills()
    {
        var cv = new CvParsedEvent
        {
            JobDescription = "Aranan: C#, Docker, Kubernetes, PostgreSQL",
            Skills = ["C#", "Docker", "PostgreSQL"],
            RawText = "C# Docker PostgreSQL"
        };

        var result = _analyzer.Analyze(cv);

        Assert.Equal(new[] { "C#", "PostgreSQL", "Docker" }, result.MatchedSkills);
        Assert.Equal(new[] { "Kubernetes" }, result.MissingSkills);
        Assert.Contains(result.Suggestions, s => s.Contains("Kubernetes"));
    }

    [Fact]
    public void Analyze_CompleteCvScoresHigherThanEmptyOne()
    {
        var job = "C#, Docker";
        var words = string.Join(' ', Enumerable.Repeat("proje", 300));

        var strong = _analyzer.Analyze(new CvParsedEvent
        {
            JobDescription = job,
            Skills = ["C#", "Docker"],
            Experience = ["Backend Developer"],
            Education = ["ITU"],
            RawText = $"ayse@example.com C# Docker {words}"
        });

        var weak = _analyzer.Analyze(new CvParsedEvent { JobDescription = job, RawText = "merhaba" });

        Assert.Equal(100, strong.AtsScore);
        Assert.Equal(0, weak.AtsScore);
        Assert.InRange(weak.Suggestions.Count, 3, 10);
    }
}
