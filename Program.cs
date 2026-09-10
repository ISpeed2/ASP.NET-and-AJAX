using FluentValidation;
using MyApi;
using MyApi.Filters;
using MyApi.Middlewares;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.WithProperty("Application", "MyApi")
        .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
        .WriteTo.Console();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllersWithViews();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Product API v1",
        Version = "v1",
        Description = "Базовая версия API продуктов"
    });

    options.SwaggerDoc("v2", new()
    {
        Title = "Product API v2",
        Version = "v2",
        Description = "Расширенная версия API продуктов"
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddAutoMapper(typeof(MappingProfile));
builder.Services.AddValidatorsFromAssemblyContaining<ProductRequestValidator>();
builder.Services.AddSingleton<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors("AllowAll");
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Product API v1");
        options.SwaggerEndpoint("/swagger/v2/swagger.json", "Product API v2");
        options.RoutePrefix = "swagger";
    });
}

app.MapGet("/", () => Results.Ok(new { message = "MyApi is running" }))
    .WithName("Root");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Age}/{action=Index}/{id?}");

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
}))
.WithName("HealthCheck");

var v1 = app.MapGroup("/api/v1/products")
    .WithTags("Products V1");

v1.MapGet("/", async (IProductService service) =>
{
    var result = await service.GetAllV1Async();
    return Results.Ok(result);
})
.WithName("GetAllProductsV1")
.Produces<IEnumerable<ProductResponseV1>>();

v1.MapGet("/{id:guid}", async (Guid id, IProductService service) =>
{
    var result = await service.GetByIdV1Async(id);

    return result == null
        ? Results.NotFound(new { error = $"Продукт с ID {id} не найден" })
        : Results.Ok(result);
})
.WithName("GetProductByIdV1")
.Produces<ProductResponseV1>()
.Produces(StatusCodes.Status404NotFound);

v1.MapPost("/", async (ProductRequest request, IProductService service) =>
{
    var result = await service.CreateV1Async(request);
    return Results.Created($"/api/v1/products/{result.Id}", result);
})
.WithName("CreateProductV1")
.AddEndpointFilter<ValidationFilter<ProductRequest>>()
.Produces<ProductResponseV1>(StatusCodes.Status201Created)
.ProducesValidationProblem();

v1.MapPut("/{id:guid}", async (Guid id, ProductRequest request, IProductService service) =>
{
    var result = await service.UpdateV1Async(id, request);

    return result == null
        ? Results.NotFound(new { error = $"Продукт с ID {id} не найден" })
        : Results.Ok(result);
})
.WithName("UpdateProductV1")
.AddEndpointFilter<ValidationFilter<ProductRequest>>()
.Produces<ProductResponseV1>()
.Produces(StatusCodes.Status404NotFound)
.ProducesValidationProblem();

v1.MapDelete("/{id:guid}", async (Guid id, IProductService service) =>
{
    var result = await service.DeleteAsync(id);

    return result
        ? Results.NoContent()
        : Results.NotFound(new { error = $"Продукт с ID {id} не найден" });
})
.WithName("DeleteProductV1")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound);

var v2 = app.MapGroup("/api/v2/products")
    .WithTags("Products V2");

v2.MapGet("/", async (IProductService service) =>
{
    var result = await service.GetAllV2Async();
    return Results.Ok(result);
})
.WithName("GetAllProductsV2")
.Produces<IEnumerable<ProductResponseV2>>();

v2.MapGet("/{id:guid}", async (Guid id, IProductService service) =>
{
    var result = await service.GetByIdV2Async(id);

    return result == null
        ? Results.NotFound(new { error = $"Продукт с ID {id} не найден" })
        : Results.Ok(result);
})
.WithName("GetProductByIdV2")
.Produces<ProductResponseV2>()
.Produces(StatusCodes.Status404NotFound);

v2.MapPost("/", async (ProductRequest request, IProductService service) =>
{
    var result = await service.CreateV2Async(request);
    return Results.Created($"/api/v2/products/{result.Id}", result);
})
.WithName("CreateProductV2")
.AddEndpointFilter<ValidationFilter<ProductRequest>>()
.Produces<ProductResponseV2>(StatusCodes.Status201Created)
.ProducesValidationProblem();

app.MapGet("/api/products/search", async (
    string category,
    IProductRepository repository,
    AutoMapper.IMapper mapper) =>
{
    var entities = await repository.GetByCategoryAsync(category);
    var result = mapper.Map<IEnumerable<ProductResponseV1>>(entities);

    return Results.Ok(result);
})
.WithName("SearchProductsByCategory")
.WithTags("Search")
.Produces<IEnumerable<ProductResponseV1>>();

app.Run();
