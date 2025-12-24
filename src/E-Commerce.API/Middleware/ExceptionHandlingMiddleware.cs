using E_Commerce.API.Domain.Exceptions;
using System.Net;
using System.Text.Json;

namespace E_Commerce.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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

        var (statusCode, errorCode, message) = exception switch
        {
            InsufficientStockException =>
                (HttpStatusCode.BadRequest, "INSUFFICIENT_STOCK", exception.Message),

            InvalidOrderStateTransitionException =>
                (HttpStatusCode.BadRequest, "INVALID_STATE_TRANSITION", exception.Message),

            DomainException =>
                (HttpStatusCode.BadRequest, "DOMAIN_ERROR", exception.Message),

            _ =>
                (HttpStatusCode.InternalServerError, "INTERNAL_ERROR", "An internal error occurred")
        };

        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            error = new
            {
                code = errorCode,
                message = message,
                timestamp = DateTime.UtcNow
            }
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}
