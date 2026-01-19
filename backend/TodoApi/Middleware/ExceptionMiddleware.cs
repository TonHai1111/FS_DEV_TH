using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TodoApi.Models.DTOs;

namespace TodoApi.Middleware;

/// <summary>
/// Global exception handling middleware for consistent error responses
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message) = exception switch
        {
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, exception.Message),
            InvalidOperationException => (HttpStatusCode.BadRequest, exception.Message),
            KeyNotFoundException => (HttpStatusCode.NotFound, exception.Message),
            ArgumentException => (HttpStatusCode.BadRequest, exception.Message),
            DbUpdateConcurrencyException => (HttpStatusCode.Conflict, "The resource was modified by another request. Please refresh and try again."),
            DbUpdateException dbEx => HandleDbUpdateException(dbEx),
            FormatException => (HttpStatusCode.BadRequest, "Invalid data format provided."),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again later.")
        };

        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse.Fail(message);

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }

    private static (HttpStatusCode statusCode, string message) HandleDbUpdateException(DbUpdateException exception)
    {
        var message = exception.InnerException?.Message ?? exception.Message;

        return message switch
        {
            var m when m.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
                    || m.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
                => (HttpStatusCode.Conflict, "A record with the same unique value already exists."),

            var m when m.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase)
                    || m.Contains("reference", StringComparison.OrdinalIgnoreCase)
                => (HttpStatusCode.BadRequest, "The operation failed due to a data relationship constraint."),

            _ => (HttpStatusCode.BadRequest, "Failed to save changes to the database.")
        };
    }
}
