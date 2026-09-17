using Microsoft.AspNetCore.Mvc;
using ASP3.Models;

namespace ASP3.Controllers;

public class HomeController : Controller
{
    [HttpGet]
    public IActionResult Index() => View(new QuestionnaireRequest());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(QuestionnaireRequest request)
    {
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        return View("Result", request);
    }

    public IActionResult About() => View();

    public IActionResult Error() => View();
}