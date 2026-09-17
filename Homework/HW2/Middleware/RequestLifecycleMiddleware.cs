using System.Diagnostics;

namespace MiddlewareHomework.Middleware;

public sealed class RequestLifecycleMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLifecycleMiddleware> _logger;

    public RequestLifecycleMiddleware(RequestDelegate next, ILogger<RequestLifecycleMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = context.TraceIdentifier;

        _logger.LogInformation("[1/4] Request received: {Method} {Path}, TraceId={TraceId}",
            context.Request.Method, context.Request.Path, requestId);

        context.Response.Headers["X-Lifecycle-TraceId"] = requestId;
        context.Response.Headers["X-Lifecycle-Stage"] = "Middleware before controller";
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Lifecycle-Stage"] = "Response returned from controller";
            return Task.CompletedTask;
        });

        try
        {
            await _next(context);
            _logger.LogInformation("[3/4] Controller completed: {StatusCode}, TraceId={TraceId}",
                context.Response.StatusCode, requestId);
        }
        finally
        {
            stopwatch.Stop();
            _logger.LogInformation("[4/4] Response sent: {ElapsedMilliseconds} ms, TraceId={TraceId}",
                stopwatch.ElapsedMilliseconds, requestId);
        }
    }
}