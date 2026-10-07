using SecureFilesMvc.Web.Models.ThreatModel;

namespace SecureFilesMvc.Web.Services;

public interface IThreatCatalogService
{
    IReadOnlyList<TrustBoundary> GetTrustBoundaries();
    IReadOnlyList<Threat> GetThreats();
    IReadOnlyList<SecurityBacklogItem> GetBacklog();
}
