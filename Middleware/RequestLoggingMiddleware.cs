namespace fuquizlearn_api.Middleware;

public class RequestLoggingMiddleware
{
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    private readonly RequestDelegate _next;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        var searchParams =
            context.Request.Query.Keys.Aggregate("",
                (current, key) => current + $"&{key}={context.Request.Query[key]}");
        _logger.LogInformation(
            $"Request: [{context.Request.Method}] {context.Request.Path}{searchParams}");
        await _next(context);
    }
}