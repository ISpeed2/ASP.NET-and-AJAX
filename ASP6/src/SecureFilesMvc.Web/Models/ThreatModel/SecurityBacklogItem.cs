namespace SecureFilesMvc.Web.Models.ThreatModel;

public enum BacklogStatus { Open, InProgress, Done, Accepted }
public enum BacklogPriority { Low, Medium, High, Critical }

public class SecurityBacklogItem
{
    public string Id { get; init; } = string.Empty;
    public string ThreatId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public BacklogPriority Priority { get; init; }
    public BacklogStatus Status { get; set; }
    public string MitigationSummary { get; init; } = string.Empty;
    public string Owner { get; init; } = string.Empty;
}
