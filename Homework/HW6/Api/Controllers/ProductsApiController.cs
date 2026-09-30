using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Binding;
using Shop.Api.Infrastructure;
using Shop.Api.Validation;
using Shop.Domain;
using Shop.Infrastructure;

namespace Shop.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsApiController(AppDbContext db, IdempotencyStore idempotency) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        return Ok(await db.Products.AsNoTracking().OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new ProductResponse(x.Id, x.Name, x.Price, x.Stock)).ToListAsync(ct));
    }

    [HttpGet("by-ids")]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> GetByIds(
        [FromQuery, ModelBinder(typeof(CsvGuidArrayBinder))] Guid[] ids, CancellationToken ct)
    {
        return Ok(await db.Products.AsNoTracking().Where(x => ids.Contains(x.Id))
            .Select(x => new ProductResponse(x.Id, x.Name, x.Price, x.Stock)).ToListAsync(ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var product = await db.Products.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (product is null) return Problem("Product not found", statusCode: 404);
        var etag = GetEtag(product);
        if (Request.Headers.IfNoneMatch == etag) return StatusCode(304);
        Response.Headers.ETag = etag;
        return Ok(new ProductResponse(product.Id, product.Name, product.Price, product.Stock));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        var key = Request.Headers["Idempotency-Key"].ToString();
        if (string.IsNullOrWhiteSpace(key)) return Problem("Idempotency-Key header is required", statusCode: 400);
        if (idempotency.TryGet(key, out var cached) && cached.ExpiresAt > DateTimeOffset.UtcNow)
            return StatusCode(cached.StatusCode, cached.Body);

        var product = new Product(request.Name, request.Price, request.Stock);
        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
        var response = new ProductResponse(product.Id, product.Name, product.Price, product.Stock);
        var location = Url.Action(nameof(Get), new { id = product.Id })!;
        idempotency.Save(key, new IdempotencyStore.CachedResponse(201, location, response, DateTimeOffset.UtcNow.AddHours(24)));
        return Created(location, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Replace(Guid id, [FromBody] UpdateProductRequest request, CancellationToken ct)
    {
        var product = await db.Products.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (product is null) return Problem("Product not found", statusCode: 404);
        if (!MatchesEtag(product)) return StatusCode(412, new ProblemDetails { Title = "Precondition Failed", Status = 412, Detail = "The product was changed by another client", Instance = Request.Path });
        product.Update(request.Name, request.Price, request.Stock);
        await db.SaveChangesAsync(ct);
        Response.Headers.ETag = GetEtag(product);
        return Ok(new ProductResponse(product.Id, product.Name, product.Price, product.Stock));
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Patch(Guid id, [FromBody] PatchProductRequest request, CancellationToken ct)
    {
        var product = await db.Products.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (product is null) return Problem("Product not found", statusCode: 404);
        if (!MatchesEtag(product)) return StatusCode(412, new ProblemDetails { Title = "Precondition Failed", Status = 412, Instance = Request.Path });
        product.Update(request.Name ?? product.Name, request.Price ?? product.Price, request.Stock ?? product.Stock);
        await db.SaveChangesAsync(ct);
        Response.Headers.ETag = GetEtag(product);
        return Ok(new ProductResponse(product.Id, product.Name, product.Price, product.Stock));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var product = await db.Products.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (product is null) return Problem("Product not found", statusCode: 404);
        db.Products.Remove(product);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private bool MatchesEtag(Product product) => Request.Headers.IfMatch.Count == 0 || Request.Headers.IfMatch == GetEtag(product);
    private static string GetEtag(Product product) => $"\"{product.Version}\"";
}