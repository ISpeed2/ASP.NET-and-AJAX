using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using BookStore.Api.Middleware;
using BookStore.Api.Services;
using BookStore.Api.Swagger;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<IBookRepository, InMemoryBookRepository>();
builder.Services.AddFeatureManagement();

builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = ApiVersionReader.Combine(
            new UrlSegmentApiVersionReader(),
            new HeaderApiVersionReader("api-version"),
            new QueryStringApiVersionReader("api-version"));
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        foreach (var description in provider.ApiVersionDescriptions)
        {
            var label = description.GroupName.ToUpperInvariant()
                      + (description.IsDeprecated ? " (deprecated)" : "");
            options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", label);
        }
    });
}

app.UseHttpsRedirection();
app.UseMiddleware<DeprecationMiddleware>();
app.MapControllers();

app.Run();
