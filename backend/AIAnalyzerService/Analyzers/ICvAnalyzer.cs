using Shared.Events;

namespace AIAnalyzerService.Analyzers;

public record CvAnalysis(
    int AtsScore,
    List<string> MatchedSkills,
    List<string> MissingSkills,
    List<string> Suggestions,
    string Summary,
    string ImprovedCvText);

public interface ICvAnalyzer
{
    string Name { get; }

    Task<CvAnalysis> AnalyzeAsync(CvParsedEvent cv, CancellationToken cancellationToken);
}
