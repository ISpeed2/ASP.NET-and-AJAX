namespace SecureFilesMvc.Web.Services;

public record FileValidationResult(bool IsValid, string? Error);

public interface IFileValidationService
{
    Task<FileValidationResult> ValidateAsync(
        Stream stream, string fileName, string contentType, long size, CancellationToken ct);
}
