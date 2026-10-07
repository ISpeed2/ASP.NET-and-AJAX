using Microsoft.AspNetCore.Authentication;
using SecureFilesMvc.Web.Middleware;
using SecureFilesMvc.Web.Security;
using SecureFilesMvc.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services
    .AddAuthentication(ApiKeyAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationHandler.SchemeName, _ => { });

builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, FileAccessHandler>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.ReadFiles,
        policy => policy.AddRequirements(new FileAccessRequirement("Files.Read")));
    options.AddPolicy(Policies.WriteFiles,
        policy => policy.AddRequirements(new FileAccessRequirement("Files.Write")));
});

builder.Services.AddSingleton<IThreatCatalogService, ThreatCatalogService>();
builder.Services.AddSingleton<IFileValidationService, FileValidationService>();
builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = FileValidationService.MaxSizeBytes;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseMiddleware<RequestIdMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
