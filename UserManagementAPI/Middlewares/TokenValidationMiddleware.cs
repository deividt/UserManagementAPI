using System.Net;
using System.Text.Json;

namespace UserManagementAPI.Middlewares;

public class TokenValidationMiddleware(RequestDelegate next, ILogger<TokenValidationMiddleware> logger)
{
    private readonly HashSet<string> _validTokens = new()
    {
        "valid-token-123",
        "admin-token-456",
        "user-token-789"
    };

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip token validation for development endpoints like Swagger
        if (context.Request.Path.StartsWithSegments("/swagger") ||
            context.Request.Path.StartsWithSegments("/openapi"))
        {
            await next(context);
            return;
        }

        var token = GetTokenFromRequest(context.Request);

        if (string.IsNullOrEmpty(token))
        {
            logger.LogWarning("No token provided in request");
            await HandleUnauthorizedAsync(context, "No token provided");
            return;
        }

        if (!IsValidToken(token))
        {
            logger.LogWarning("Invalid token provided: {Token}", token);
            await HandleUnauthorizedAsync(context, "Invalid token");
            return;
        }

        logger.LogInformation("Valid token provided, proceeding with request");
        await next(context);
    }

    private string? GetTokenFromRequest(HttpRequest request)
    {
        // Try to get token from Authorization header (Bearer token)
        if (request.Headers.ContainsKey("Authorization"))
        {
            var authHeader = request.Headers["Authorization"].ToString();
            if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return authHeader.Substring("Bearer ".Length).Trim();
            }
        }

        // Try to get token from custom header
        if (request.Headers.ContainsKey("X-Token"))
        {
            return request.Headers["X-Token"].ToString();
        }

        // Try to get token from query parameter
        if (request.Query.ContainsKey("token"))
        {
            return request.Query["token"].ToString();
        }

        return null;
    }

    private bool IsValidToken(string token)
    {
        // In a real application, this would validate against a database,
        // JWT validation, or external authentication service
        return _validTokens.Contains(token);
    }

    private static async Task HandleUnauthorizedAsync(HttpContext context, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;

        var response = new
        {
            error = message,
            statusCode = 401
        };

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}
