using System.ComponentModel.DataAnnotations;

namespace SecureFilesMvc.Web.Models.Files;

public class FileUploadRequest
{
    [Required]
    public IFormFile? File { get; init; }
}
