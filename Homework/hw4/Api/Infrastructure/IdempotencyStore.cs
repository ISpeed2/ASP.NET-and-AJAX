using System.Collections.Concurrent;

namespace Shop.Api.Infrastructure;

public sealed class IdempotencyStore
{
    private readonly ConcurrentDictionary<string, CachedResponse> responses = new();

    public bool TryGet(string key, out CachedResponse response) => responses.TryGetValue(key, out response!);

    public void Save(string key, CachedResponse response) => responses[key] = response;

    public sealed record CachedResponse(int StatusCode, string Location, object Body, DateTimeOffset ExpiresAt);
}