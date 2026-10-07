using SecureFilesMvc.Web.Models.ThreatModel;

namespace SecureFilesMvc.Web.Models.ViewModels;

public class ThreatModelViewModel
{
    public IReadOnlyList<TrustBoundary> Boundaries { get; init; } = Array.Empty<TrustBoundary>();
    public IReadOnlyList<Threat> Threats { get; init; } = Array.Empty<Threat>();
    public IReadOnlyList<SecurityBacklogItem> Backlog { get; init; } = Array.Empty<SecurityBacklogItem>();
}
