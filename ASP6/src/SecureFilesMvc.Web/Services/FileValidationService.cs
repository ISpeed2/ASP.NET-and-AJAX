namespace SecureFilesMvc.Web.Services;

public class FileValidationService : IFileValidationService
{
    public const long MaxSizeBytes = 10 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".gif", ".pdf", ".txt" };

    private static readonly Dictionary<string, byte[][]> Signatures = new()
    {
        [".png"] = new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47 } },
        [".jpg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
        [".jpeg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
        [".gif"] = new[] { new byte[] { 0x47, 0x49, 0x46, 0x38 } },
        [".pdf"] = new[] { new byte[] { 0x25, 0x50, 0x44, 0x46 } }
    };

    public async Task<FileValidationResult> ValidateAsync(
        Stream stream, string fileName, string contentType, long size, CancellationToken ct)
    {
        if (size <= 0) return new(false, "Пустой файл");
        if (size > MaxSizeBytes) return new(false, $"Файл больше {MaxSizeBytes / 1024 / 1024} МБ");

        var ext = Path.GetExtension(Path.GetFileName(fileName));
        if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
            return new(false, $"Расширение '{ext}' не разрешено");

        if (!stream.CanSeek)
            return new(false, "Поток файла не поддерживает проверку");

        var header = new byte[8];
        var read = await stream.ReadAsync(header.AsMemory(), ct);
        stream.Position = 0;
        if (read == 0) return new(false, "Пустой файл");

        var extLower = ext.ToLowerInvariant();
        if (extLower == ".txt")
        {
            if (header.Take(read).Any(value => value == 0 || value < 0x09 || value is > 0x0D and < 0x20))
                return new(false, "Содержимое не является текстом");
        }
        else if (Signatures.TryGetValue(extLower, out var signatures))
        {
            if (!signatures.Any(signature =>
                    read >= signature.Length && header.AsSpan(0, signature.Length).SequenceEqual(signature)))
                return new(false, "Сигнатура файла не соответствует расширению");
        }

        return new(true, null);
    }
}
