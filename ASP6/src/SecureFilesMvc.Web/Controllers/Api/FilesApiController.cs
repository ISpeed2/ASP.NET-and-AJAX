using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureFilesMvc.Web.Models.Files;
using SecureFilesMvc.Web.Security;
using SecureFilesMvc.Web.Services;

namespace SecureFilesMvc.Web.Controllers.Api;

[ApiController]
[Route("api/files")]
[Authorize(AuthenticationSchemes = ApiKeyAuthenticationHandler.SchemeName)]
public class FilesApiController(
    IFileStorageService storage,
    IFileValidationService validator,
    ILogger<FilesApiController> logger) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = Policies.WriteFiles)]
    [RequestSizeLimit(FileValidationService.MaxSizeBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = FileValidationService.MaxSizeBytes)]
    public async Task<IActionResult> Upload([FromForm] FileUploadRequest request, CancellationToken ct)
    {
        var file = request.File;
        if (file is null || file.Length == 0)
            return BadRequest(Error("no_file", "Файл не передан"));

        var claimedType = string.IsNullOrWhiteSpace(file.ContentType)
            ? "application/octet-stream"
            : file.ContentType;

        await using var stream = file.OpenReadStream();
        var validation = await validator.ValidateAsync(
            stream, file.FileName, claimedType, file.Length, ct);
        if (!validation.IsValid)
        {
            logger.LogWarning("Upload rejected: {Reason} Name={Name}", validation.Error, file.FileName);
            return BadRequest(Error("validation_failed", validation.Error ?? "Файл не прошёл проверку"));
        }

        var stored = await storage.SaveAsync(stream, file.FileName, claimedType, ct);
        var response = new FileMetaDto(
            stored.Id, stored.OriginalName, stored.Size, stored.ContentType);
        return CreatedAtAction(nameof(Download), new { id = stored.Id }, response);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = Policies.ReadFiles)]
    public async Task<IActionResult> Download(string id, CancellationToken ct)
    {
        var result = await storage.OpenAsync(id, ct);
        if (result is null)
            return NotFound(Error("not_found", "Файл не найден"));

        var (stream, meta) = result.Value;
        Response.Headers.ContentDisposition =
            $"attachment; filename*=UTF-8''{Uri.EscapeDataString(meta.OriginalName)}";
        return File(stream, meta.ContentType, enableRangeProcessing: true);
    }

    private ApiError Error(string code, string message) =>
        new(code, message, HttpContext.Items["RequestId"]?.ToString());
}
