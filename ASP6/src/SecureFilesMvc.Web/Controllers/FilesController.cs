using Microsoft.AspNetCore.Mvc;

namespace SecureFilesMvc.Web.Controllers;

public class FilesController : Controller
{
    public IActionResult Index() => View();
}
