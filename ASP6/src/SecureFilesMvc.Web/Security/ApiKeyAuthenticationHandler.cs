using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace SecureFilesMvc.Web.Security;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "ApiKey";
    public const string HeaderName = "X-Api-Key";
    private readonly IConfiguration _configuration;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        _configuration = configuration;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var provided))
            return Task.FromResult(AuthenticateResult.NoResult());

        var validKey = _configuration["Security:ApiKey"];
        if (string.IsNullOrEmpty(validKey) || !CryptographicEquals(provided.ToString(), validKey))
        {
            Logger.LogWarning("Invalid API key attempt from {RemoteIp}", Context.Connection.RemoteIpAddress);
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "api-client"),
            new Claim(ClaimTypes.Name, "API Client"),
            new Claim(ClaimTypes.Role, "Files.Read"),
            new Claim(ClaimTypes.Role, "Files.Write")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(principal, SchemeName)));
    }

    private static bool CryptographicEquals(string provided, string expected)
    {
        var providedBytes = Encoding.UTF8.GetBytes(provided);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        return providedBytes.Length == expectedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }
}
