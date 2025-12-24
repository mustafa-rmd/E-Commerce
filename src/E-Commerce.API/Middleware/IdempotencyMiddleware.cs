using E_Commerce.API.Domain.Entities;
using E_Commerce.API.Repositories.Interfaces;
using System.Text;

namespace E_Commerce.API.Middleware;

/// <summary>
/// Middleware to handle idempotent requests using X-Idempotency-Key header
/// </summary>
public class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<IdempotencyMiddleware> _logger;
    private const string IdempotencyKeyHeader = "X-Idempotency-Key";

    public IdempotencyMiddleware(RequestDelegate next, ILogger<IdempotencyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IUnitOfWork unitOfWork)
    {
        // Only process POST and PATCH requests (idempotent operations)
        if (context.Request.Method != HttpMethods.Post && context.Request.Method != HttpMethods.Patch)
        {
            await _next(context);
            return;
        }

        // Check for idempotency key header
        if (!context.Request.Headers.TryGetValue(IdempotencyKeyHeader, out var idempotencyKey) ||
            string.IsNullOrWhiteSpace(idempotencyKey))
        {
            // No idempotency key provided - proceed normally
            await _next(context);
            return;
        }

        var requestPath = context.Request.Path.ToString();
        var requestMethod = context.Request.Method;

        _logger.LogInformation("Idempotency check for key: {IdempotencyKey}, path: {Path}, method: {Method}",
            idempotencyKey, requestPath, requestMethod);

        // Check if we have a cached response
        var existingRecord = await unitOfWork.IdempotencyRecords.GetByKeyAsync(
            idempotencyKey!, requestPath, requestMethod);

        if (existingRecord != null)
        {
            // Check if record is expired
            if (existingRecord.IsExpired())
            {
                _logger.LogWarning("Idempotency record expired for key: {IdempotencyKey}", idempotencyKey);
                // Delete expired record and process as new request
                await unitOfWork.IdempotencyRecords.DeleteAsync(existingRecord);
                await unitOfWork.SaveChangesAsync();
            }
            else
            {
                // Return cached response
                _logger.LogInformation("Returning cached response for idempotency key: {IdempotencyKey}", idempotencyKey);

                context.Response.StatusCode = existingRecord.ResponseStatusCode;
                context.Response.ContentType = existingRecord.ResponseContentType;
                await context.Response.WriteAsync(existingRecord.ResponseBody);
                return;
            }
        }

        // No cached response - process the request and cache the response
        await ProcessAndCacheResponseAsync(context, unitOfWork, idempotencyKey!, requestPath, requestMethod);
    }

    private async Task ProcessAndCacheResponseAsync(
        HttpContext context,
        IUnitOfWork unitOfWork,
        string idempotencyKey,
        string requestPath,
        string requestMethod)
    {
        // Read and cache the request body
        context.Request.EnableBuffering();
        string requestBody = await ReadRequestBodyAsync(context.Request);

        // Replace the response body stream to capture the response
        var originalBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            // Process the request
            await _next(context);

            // Read the response body
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
            responseBodyStream.Seek(0, SeekOrigin.Begin);

            // Only cache successful responses (2xx status codes)
            if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
            {
                // Create idempotency record
                var now = DateTime.UtcNow;
                var idempotencyRecord = new IdempotencyRecord
                {
                    IdempotencyKey = idempotencyKey,
                    RequestPath = requestPath,
                    RequestMethod = requestMethod,
                    RequestBody = requestBody,
                    ResponseStatusCode = context.Response.StatusCode,
                    ResponseBody = responseBody,
                    ResponseContentType = context.Response.ContentType ?? "application/json",
                    CreatedAt = now,
                    UpdatedAt = now
                };

                await unitOfWork.IdempotencyRecords.AddAsync(idempotencyRecord);
                await unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Cached response for idempotency key: {IdempotencyKey}", idempotencyKey);
            }
            else
            {
                _logger.LogWarning("Not caching non-successful response (status {StatusCode}) for idempotency key: {IdempotencyKey}",
                    context.Response.StatusCode, idempotencyKey);
            }

            // Copy the response body back to the original stream
            await responseBodyStream.CopyToAsync(originalBodyStream);
        }
        finally
        {
            // Restore original body stream
            context.Response.Body = originalBodyStream;
        }
    }

    private async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        request.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Seek(0, SeekOrigin.Begin);
        return body;
    }
}
