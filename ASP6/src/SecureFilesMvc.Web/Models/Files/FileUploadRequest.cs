using System.ComponentModel.DataAnnotations;

namespace SecureFilesMvc.Web.Models.Files;

public class FileUploadRequest
{
    [Required]
    public IFormFile? File { get; init; }

    [StringLength(500)]
    public string? Description { get; init; }

    public bool ConfirmLicense { get; init; }
}
