using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Shop Management API",
        Version = "1.0.0",
        Description = "API для управления каталогом товаров интернет-магазина.",
        Contact = new OpenApiContact
        {
            Name = "Shop API Support",
            Email = "support@shop-api.example"
        },
        License = new OpenApiLicense
        {
            Name = "MIT",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Shop Management API v1");
    options.RoutePrefix = "swagger";
});

var products = new List<CatalogProductResponse>
{
    new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Клавиатура", 4500m, 10),
    new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Мышь", 2500m, 20),
    new(Guid.Parse("33333333-3333-3333-3333-333333333333"), "Монитор", 25000m, 5)
};

app.MapGet("/api/products", () => Results.Ok(products))
    .WithName("GetProducts")
    .WithTags("Products")
    .WithSummary("Получить каталог товаров")
    .Produces<IReadOnlyList<CatalogProductResponse>>(StatusCodes.Status200OK);

app.MapGet("/api/products/{id:guid}", (Guid id) =>
    products.FirstOrDefault(product => product.Id == id) is { } product
        ? Results.Ok(product)
        : Results.NotFound(new { message = $"Product {id} not found" }))
    .WithName("GetProductById")
    .WithTags("Products")
    .WithSummary("Получить товар по идентификатору")
    .Produces<CatalogProductResponse>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound);

app.MapPost("/api/products", (CreateCatalogProductRequest request) =>
{
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(request.Name))
        errors["name"] = ["Название обязательно."];
    if (request.Price <= 0)
        errors["price"] = ["Цена должна быть больше нуля."];
    if (request.Stock < 0)
        errors["stock"] = ["Остаток не может быть отрицательным."];

    if (errors.Count > 0)
        return Results.ValidationProblem(errors);

    var product = new CatalogProductResponse(
        Guid.NewGuid(),
        request.Name.Trim(),
        request.Price,
        request.Stock);

    products.Add(product);
    return Results.Created($"/api/products/{product.Id}", product);
})
    .WithName("CreateProduct")
    .WithTags("Products")
    .WithSummary("Добавить товар в каталог")
    .Produces<CatalogProductResponse>(StatusCodes.Status201Created)
    .ProducesValidationProblem();

app.MapGet("/", () => Results.Redirect("/swagger"))
    .ExcludeFromDescription();

app.Run();

public sealed record CatalogProductResponse(Guid Id, string Name, decimal Price, int Stock);
public sealed record CreateCatalogProductRequest(string Name, decimal Price, int Stock);

public partial class Program;
