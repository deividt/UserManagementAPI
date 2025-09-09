namespace UserManagementAPI.Middlewares;

public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Log request information
        var method = context.Request.Method;
        var path = context.Request.Path;

        logger.LogInformation("Request: {Method} {Path}", method, path);

        // Call the next middleware in the pipeline
        await next(context);

        // Log response information
        var statusCode = context.Response.StatusCode;

        logger.LogInformation("Response: {Method} {Path} - Status: {StatusCode}", method, path, statusCode);
    }
}