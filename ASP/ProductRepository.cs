namespace MyApi;

/// <summary>
/// Простая реализация репозитория продуктов в памяти.
/// </summary>
public class ProductRepository : IProductRepository
{
    private readonly List<ProductEntity> _products = new();

    public Task<IEnumerable<ProductEntity>> GetAllAsync(bool includeDeleted = false)
    {
        var products = _products.AsEnumerable();

        if (!includeDeleted)
        {
            products = products.Where(product => product.DeletedAt == null);
        }

        return Task.FromResult(products);
    }

    public Task<ProductEntity?> GetByIdAsync(Guid id, bool includeDeleted = false)
    {
        var product = _products.FirstOrDefault(product =>
            product.Id == id && (includeDeleted || product.DeletedAt == null));

        return Task.FromResult(product);
    }

    public Task<IEnumerable<ProductEntity>> GetByCategoryAsync(string category)
    {
        var products = _products
            .Where(product =>
                product.DeletedAt == null &&
                string.Equals(product.Category, category, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(products);
    }

    public Task<ProductEntity> CreateAsync(ProductEntity entity)
    {
        entity.Id = entity.Id == Guid.Empty ? Guid.NewGuid() : entity.Id;
        entity.CreatedAt = DateTime.UtcNow;
        entity.DeletedAt = null;

        _products.Add(entity);

        return Task.FromResult(entity);
    }

    public Task<ProductEntity> UpdateAsync(ProductEntity entity)
    {
        var existingProduct = _products.FirstOrDefault(product => product.Id == entity.Id);

        if (existingProduct == null)
        {
            throw new InvalidOperationException("Продукт не найден");
        }

        existingProduct.Name = entity.Name;
        existingProduct.Description = entity.Description;
        existingProduct.PriceInCents = entity.PriceInCents;
        existingProduct.Category = entity.Category;
        existingProduct.StockQuantity = entity.StockQuantity;
        existingProduct.IsActive = entity.IsActive;
        existingProduct.Attributes = entity.Attributes;
        existingProduct.UpdatedAt = DateTime.UtcNow;

        return Task.FromResult(existingProduct);
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        var product = _products.FirstOrDefault(product => product.Id == id && product.DeletedAt == null);

        if (product == null)
        {
            return Task.FromResult(false);
        }

        product.DeletedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;

        return Task.FromResult(true);
    }

    public Task<bool> ExistsAsync(Guid id)
    {
        var exists = _products.Any(product => product.Id == id && product.DeletedAt == null);

        return Task.FromResult(exists);
    }

    public Task<int> CountAsync()
    {
        var count = _products.Count(product => product.DeletedAt == null);

        return Task.FromResult(count);
    }
}
