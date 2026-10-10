namespace HW9.Models;

public sealed class LoginForm
{
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public bool RememberMe { get; init; }
}

public sealed record TokenRequest(string Username, string Password);
public sealed record RefreshRequest(string RefreshToken);

public sealed record TokenResponse(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    int ExpiresIn);

public sealed record UserAccount(string Username, string PasswordHash, string[] Roles);
