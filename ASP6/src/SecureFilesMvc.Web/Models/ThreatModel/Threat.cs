namespace SecureFilesMvc.Web.Models.ThreatModel;

public class Threat
{
    public string Id { get; init; } = string.Empty;
    public StrideCategory Category { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string TrustBoundaryId { get; init; } = string.Empty;
    public double RiskScore { get; init; }
    public IReadOnlyList<string> Mitigations { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> BacklogItemIds { get; init; } = Array.Empty<string>();
}
