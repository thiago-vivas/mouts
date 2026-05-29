using System.Text.Json;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.WebApi.Middleware;

/// <summary>
/// Central error handler that maps exceptions to the API's documented error
/// format (<c>{ type, error, detail }</c>, see .doc/general-api.md). Covers
/// validation errors, resource-not-found, domain rule violations, auth failures
/// and unexpected errors.
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            var detail = string.Join("; ", ex.Errors.Select(e => e.ErrorMessage));
            await WriteAsync(context, StatusCodes.Status400BadRequest,
                "ValidationError", "Invalid input data", detail);
        }
        catch (KeyNotFoundException ex)
        {
            await WriteAsync(context, StatusCodes.Status404NotFound,
                "ResourceNotFound", "Resource not found", ex.Message);
        }
        catch (DomainException ex)
        {
            await WriteAsync(context, StatusCodes.Status400BadRequest,
                "DomainRuleViolation", "Business rule violation", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            await WriteAsync(context, StatusCodes.Status400BadRequest,
                "InvalidOperation", "Operation not allowed", ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            await WriteAsync(context, StatusCodes.Status401Unauthorized,
                "AuthenticationError", "Authentication failed", ex.Message);
        }
        catch (Exception ex)
        {
            // Log the full exception server-side, but never leak internal details
            // (messages can contain connection strings, stack info, etc.) to the client.
            _logger.LogError(ex, "Unhandled exception");
            await WriteAsync(context, StatusCodes.Status500InternalServerError,
                "ServerError", "An unexpected error occurred",
                "An unexpected error occurred while processing your request.");
        }
    }

    private static Task WriteAsync(HttpContext context, int statusCode,
        string type, string error, string detail)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var payload = new { type, error, detail };
        return context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }
}
