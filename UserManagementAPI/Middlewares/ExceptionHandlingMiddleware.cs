using System.Net;
using System.Text.Json;

namespace UserManagementAPI.Middlewares;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = GetStatusCode(exception);

        var response = new
        {
            error = GetErrorMessage(exception, context.Response.StatusCode)
        };

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }

    private static int GetStatusCode(Exception exception)
    {
        return exception switch
        {
            ArgumentException => (int)HttpStatusCode.BadRequest,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            NotImplementedException => (int)HttpStatusCode.NotImplemented,
            KeyNotFoundException => (int)HttpStatusCode.NotFound,
            _ => (int)HttpStatusCode.InternalServerError
        };
    }

    private static string GetErrorMessage(Exception exception, int statusCode)
    {
        return statusCode switch
        {
            (int)HttpStatusCode.BadRequest => "Bad request.",
            (int)HttpStatusCode.Unauthorized => "Unauthorized access.",
            (int)HttpStatusCode.NotFound => "Resource not found.",
            (int)HttpStatusCode.NotImplemented => "Not implemented.",
            _ => "Internal server error."
        };
    }
}
