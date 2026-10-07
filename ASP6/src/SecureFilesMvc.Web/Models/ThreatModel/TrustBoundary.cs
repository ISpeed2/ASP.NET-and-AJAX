namespace SecureFilesMvc.Web.Models.ThreatModel;

public class TrustBoundary
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string CrossingData { get; init; } = string.Empty;
    public IReadOnlyList<string> Controls { get; init; } = Array.Empty<string>();
}
