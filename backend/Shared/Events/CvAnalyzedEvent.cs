namespace Shared.Events;

public class CvAnalyzedEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public int AtsScore { get; set; }
    public List<string> MissingSkills { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
    public string ImprovedCvText { get; set; } = string.Empty;
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}