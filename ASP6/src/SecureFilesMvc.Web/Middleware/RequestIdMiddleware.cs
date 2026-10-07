namespace SecureFilesMvc.Web.Middleware;

public class RequestIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ILogger<RequestIdMiddleware> logger)
    {
        var requestId = context.Request.Headers["X-Request-Id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(requestId) || requestId.Length > 64)
            requestId = Guid.NewGuid().ToString("N");

        context.Items["RequestId"] = requestId;
        context.Response.Headers["X-Request-Id"] = requestId;

        using (logger.BeginScope(new Dictionary<string, object> { ["RequestId"] = requestId }))
        {
            await next(context);
        }
    }
}
