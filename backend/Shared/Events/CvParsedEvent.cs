namespace Shared.Events;

public class CvParsedEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string JobDescription { get; set; } = string.Empty;
    public List<string> Skills { get; set; } = new();
    public List<string> Experience { get; set; } = new();
    public List<string> Education { get; set; } = new();
    public string RawText { get; set; } = string.Empty;
    public DateTime ParsedAt { get; set; } = DateTime.UtcNow;
}