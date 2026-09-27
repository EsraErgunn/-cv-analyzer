namespace ResultService.Data;

public class CvAnalysisResult
{
    // Upload sırasında üretilen CvId; istemci sonucu bu kimlikle sorgular.
    public Guid CvId { get; set; }
    public Guid UserId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int AtsScore { get; set; }
    public List<string> MatchedSkills { get; set; } = new();
    public List<string> MissingSkills { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
    public string ImprovedCvText { get; set; } = string.Empty;
    public string AnalyzerName { get; set; } = string.Empty;
    public DateTime AnalyzedAt { get; set; }
}
