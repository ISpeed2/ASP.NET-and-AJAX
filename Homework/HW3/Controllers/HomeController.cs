using HW3.Models;
using HW3.Services;
using Microsoft.AspNetCore.Mvc;

namespace HW3.Controllers;

public class HomeController : Controller
{
    private readonly IProductRepository _products;

    public HomeController(IProductRepository products)
    {
        _products = products;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var products = await _products.GetAllAsync(ct);
        return View(products.OrderBy(product => product.Id).ToList());
    }

    [HttpGet]
    public IActionResult Create() => View(new Product());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product product, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(product);
        }

        var savedProduct = await _products.AddAsync(product, ct);
        return RedirectToAction(nameof(Details), new { id = savedProduct.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var product = await _products.GetByIdAsync(id, ct);
        return product is null ? NotFound() : View(product);
    }
}
