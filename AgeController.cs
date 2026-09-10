using Microsoft.AspNetCore.Mvc;

namespace MyApi;

public sealed class AgeController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(string userName, int age)
    {
        if (string.IsNullOrWhiteSpace(userName) || age is < 0 or > 120)
        {
            ModelState.AddModelError(string.Empty, "Введите имя и корректный возраст от 0 до 120 лет.");
            return View();
        }

        return View("Result", new AgeResultModel
        {
            UserName = userName.Trim(),
            Age = age
        });
    }
}
