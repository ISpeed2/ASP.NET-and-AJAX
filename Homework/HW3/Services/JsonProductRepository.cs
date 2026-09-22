using System.Text.Json;
using HW3.Models;

namespace HW3.Services;

public sealed class JsonProductRepository : IProductRepository
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public JsonProductRepository(IWebHostEnvironment environment)
    {
        _filePath = Path.Combine(environment.ContentRootPath, "Data", "products.json");
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default)
    {
        await _mutex.WaitAsync(ct);
        try
        {
            return await LoadAsync(ct);
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<Product?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var products = await GetAllAsync(ct);
        return products.FirstOrDefault(product => product.Id == id);
    }

    public async Task<Product> AddAsync(Product product, CancellationToken ct = default)
    {
        await _mutex.WaitAsync(ct);
        try
        {
            var products = await LoadAsync(ct);
            product.Id = products.Count == 0 ? 1 : products.Max(item => item.Id) + 1;
            products.Add(product);

            await using var stream = File.Create(_filePath);
            await JsonSerializer.SerializeAsync(stream, products, _options, ct);
            return product;
        }
        finally
        {
            _mutex.Release();
        }
    }

    private async Task<List<Product>> LoadAsync(CancellationToken ct)
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        await using var stream = File.OpenRead(_filePath);
        return await JsonSerializer.DeserializeAsync<List<Product>>(stream, _options, ct) ?? [];
    }
}
