using AutoMapper;

namespace MyApi;

/// <summary>
/// Бизнес-логика для работы с продуктами.
/// </summary>
public class ProductService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly IMapper _mapper;

    public ProductService(IProductRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ProductResponseV1>> GetAllV1Async()
    {
        var products = await _repository.GetAllAsync();
        return _mapper.Map<IEnumerable<ProductResponseV1>>(products);
    }

    public async Task<ProductResponseV1?> GetByIdV1Async(Guid id)
    {
        var product = await _repository.GetByIdAsync(id);
        return product == null ? null : _mapper.Map<ProductResponseV1>(product);
    }

    public async Task<ProductResponseV1> CreateV1Async(ProductRequest request)
    {
        var entity = _mapper.Map<ProductEntity>(request);
        var product = await _repository.CreateAsync(entity);
        return _mapper.Map<ProductResponseV1>(product);
    }

    public async Task<ProductResponseV1?> UpdateV1Async(Guid id, ProductRequest request)
    {
        var existingProduct = await _repository.GetByIdAsync(id);

        if (existingProduct == null)
        {
            return null;
        }

        _mapper.Map(request, existingProduct);
        existingProduct.Id = id;

        var product = await _repository.UpdateAsync(existingProduct);
        return _mapper.Map<ProductResponseV1>(product);
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        return _repository.DeleteAsync(id);
    }

    public async Task<IEnumerable<ProductResponseV2>> GetAllV2Async()
    {
        var products = await _repository.GetAllAsync();
        return _mapper.Map<IEnumerable<ProductResponseV2>>(products);
    }

    public async Task<ProductResponseV2?> GetByIdV2Async(Guid id)
    {
        var product = await _repository.GetByIdAsync(id);
        return product == null ? null : _mapper.Map<ProductResponseV2>(product);
    }

    public async Task<ProductResponseV2> CreateV2Async(ProductRequest request)
    {
        var entity = _mapper.Map<ProductEntity>(request);
        var product = await _repository.CreateAsync(entity);
        return _mapper.Map<ProductResponseV2>(product);
    }
}
