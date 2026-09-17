using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MiddlewareHomework.Models;

namespace MiddlewareHomework.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View(new RegistrationRequest());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(RegistrationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        return View("Success", request);
    }

    public IActionResult Lifecycle()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
