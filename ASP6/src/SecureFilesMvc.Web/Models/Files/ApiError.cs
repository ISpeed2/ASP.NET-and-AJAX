namespace SecureFilesMvc.Web.Models.Files;

public record ApiError(string Code, string Message, string? CorrelationId = null);
