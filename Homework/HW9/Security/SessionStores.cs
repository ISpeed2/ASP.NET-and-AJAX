using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace HW9.Security;

public sealed class CookieSessionStore
{
    private readonly ConcurrentDictionary<string, byte> _active = new();

    public void Add(string sessionId) => _active[sessionId] = 0;
    public bool IsActive(string sessionId) => _active.ContainsKey(sessionId);
    public void Revoke(string sessionId) => _active.TryRemove(sessionId, out _);
}

public sealed class RefreshTokenStore
{
    private readonly ConcurrentDictionary<string, RefreshTokenEntry> _tokens = new();

    public void Save(string rawToken, string username, DateTimeOffset expiresAt) =>
        _tokens[Hash(rawToken)] = new RefreshTokenEntry(username, expiresAt);

    public RefreshTokenEntry? TakeValid(string rawToken)
    {
        if (!_tokens.TryRemove(Hash(rawToken), out var entry))
            return null;

        return entry.ExpiresAt > DateTimeOffset.UtcNow ? entry : null;
    }

    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));
}

public sealed record RefreshTokenEntry(string Username, DateTimeOffset ExpiresAt);
