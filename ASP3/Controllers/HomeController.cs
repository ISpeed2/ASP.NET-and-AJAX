using Microsoft.AspNetCore.Mvc;
using ShopShell.Services;
using ShopShell.ViewModels;

namespace ShopShell.Controllers;

public class HomeController : Controller
{
    private readonly IProductRepository _repo;

    public HomeController(IProductRepository repo) => _repo = repo;

    // UI-экшен: возвращает View (shell)
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        // Берём только категории для фильтров — не весь список товаров
        var all = await _repo.GetAllAsync(ct);
        var categories = all.Select(p => p.Category).Distinct().OrderBy(c => c).ToList();

        var vm = new ProductListViewModel
        {
            Title = "Каталог товаров",
            ApiEndpoint = Url.Action("List", "Products")!,  // /Products/List
            Categories = categories
        };

        return View(vm);   // Razor получает только метаданные
    }

    public IActionResult About() => View();

    public IActionResult Error() => View();
}