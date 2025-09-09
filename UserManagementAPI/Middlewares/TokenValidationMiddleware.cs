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
        // Debug logging to see all headers
        logger.LogInformation("Request headers: {Headers}",
            string.Join(", ", request.Headers.Select(h => $"{h.Key}={h.Value}")));

        // Try to get token from Authorization header (Bearer token)
        var authHeaders = request.Headers.Where(h =>
            string.Equals(h.Key, "Authorization", StringComparison.OrdinalIgnoreCase));

        foreach (var header in authHeaders)
        {
            var authHeaderValue = header.Value.ToString();
            logger.LogInformation("Found Authorization header: {AuthHeader}", authHeaderValue);

            if (string.IsNullOrEmpty(authHeaderValue) ||
                !authHeaderValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                continue;

            var token = authHeaderValue.Substring("Bearer ".Length).Trim();
            logger.LogInformation("Extracted token: {Token}", token);
            return token;
        }

        // Try to get token from custom header
        var xTokenHeaders = request.Headers.Where(h =>
            string.Equals(h.Key, "X-Token", StringComparison.OrdinalIgnoreCase));

        foreach (var header in xTokenHeaders)
        {
            var tokenValue = header.Value.ToString();
            if (string.IsNullOrEmpty(tokenValue))
                continue;

            logger.LogInformation("Found X-Token header: {Token}", tokenValue);
            return tokenValue;
        }

        // Try to get token from query parameter
        if (request.Query.ContainsKey("token"))
        {
            var tokenValue = request.Query["token"].ToString();
            if (!string.IsNullOrEmpty(tokenValue))
            {
                logger.LogInformation("Found token in query parameter: {Token}", tokenValue);
                return tokenValue;
            }
        }

        logger.LogWarning("No token found in any expected location");
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