using Microsoft.AspNetCore.Mvc;
using SecureFilesMvc.Web.Models.ViewModels;
using SecureFilesMvc.Web.Services;

namespace SecureFilesMvc.Web.Controllers;

public class ThreatsController(IThreatCatalogService catalog) : Controller
{
    public IActionResult Index() => View(new ThreatModelViewModel
    {
        TrustBoundaries = catalog.GetTrustBoundaries(),
        Threats = catalog.GetThreats(),
        Backlog = catalog.GetBacklog()
    });
}
