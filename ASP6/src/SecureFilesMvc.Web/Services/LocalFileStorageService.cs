using System.Collections.Concurrent;

namespace SecureFilesMvc.Web.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _root;
    private readonly string _rootWithSeparator;
    private readonly ILogger<LocalFileStorageService> _logger;
    private readonly ConcurrentDictionary<string, StoredFile> _index = new();

    public LocalFileStorageService(IWebHostEnvironment environment, ILogger<LocalFileStorageService> logger)
    {
        _logger = logger;
        _root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "App_Data", "uploads"));
        _rootWithSeparator = _root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(_root);
    }

    public async Task<StoredFile> SaveAsync(
        Stream content, string originalName, string contentType, CancellationToken ct)
    {
        var id = Guid.NewGuid().ToString("N");
        var ext = Path.GetExtension(Path.GetFileName(originalName));
        if (string.IsNullOrWhiteSpace(ext) || ext.Length > 10)
            ext = ".bin";

        var resolved = ResolveInsideRoot(id + ext.ToLowerInvariant());
        await using (var output = new FileStream(
                         resolved, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                         bufferSize: 81920, useAsync: true))
        {
            await content.CopyToAsync(output, ct);
        }

        var safeOriginalName = Path.GetFileName(originalName);
        var meta = new StoredFile(
            id, safeOriginalName, resolved, new FileInfo(resolved).Length, contentType);
        _index[id] = meta;
        _logger.LogInformation("File stored. Id={Id} Size={Size} Type={Type}", id, meta.Size, contentType);
        return meta;
    }

    public Task<(Stream Stream, StoredFile Meta)?> OpenAsync(string id, CancellationToken ct)
    {
        if (!IsValidId(id) || !_index.TryGetValue(id, out var meta))
            return Task.FromResult<(Stream, StoredFile)?>(null);

        var resolved = Path.GetFullPath(meta.StoredPath);
        if (!IsInsideRoot(resolved) || !File.Exists(resolved))
            return Task.FromResult<(Stream, StoredFile)?>(null);

        Stream stream = new FileStream(
            resolved, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 81920, useAsync: true);
        return Task.FromResult<(Stream, StoredFile)?>((stream, meta));
    }

    public Task<bool> ExistsAsync(string id, CancellationToken ct) =>
        Task.FromResult(IsValidId(id) && _index.ContainsKey(id));

    private string ResolveInsideRoot(string fileName)
    {
        var resolved = Path.GetFullPath(Path.Combine(_root, fileName));
        if (!IsInsideRoot(resolved))
            throw new InvalidOperationException("Path traversal detected");
        return resolved;
    }

    private bool IsInsideRoot(string path) =>
        path.StartsWith(_rootWithSeparator, StringComparison.OrdinalIgnoreCase);

    private static bool IsValidId(string id) =>
        id.Length == 32 && id.All(Uri.IsHexDigit);
}
