using System.Diagnostics;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Mootable.Application.Pipelines.Logging;

/// <summary>
/// Audit logging için MediatR behavior.
/// 
/// LOGLANAN BİLGİLER:
/// - Request type ve payload
/// - User ID (JWT'den)
/// - IP Address
/// - Execution time
/// - Exception (varsa)
/// 
/// PRODUCTION DENEYİMİ:
/// "Kim bu veriyi sildi?" sorusuna cevap veremiyorsanız, audit logging eksiktir.
/// GDPR/KVKK compliance için de bu loglar zorunlu.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LoggingBehavior(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        if (request is not ILoggableRequest loggableRequest)
        {
            return await next();
        }

        var requestName = typeof(TRequest).Name;
        var userId = GetUserId();
        var ipAddress = GetIpAddress();
        var stopwatch = Stopwatch.StartNew();

        if (loggableRequest.LogRequest)
        {
            var sanitizedRequest = SanitizeRequest(request);
            _logger.LogInformation(
                "Executing {RequestName} | User: {UserId} | IP: {IpAddress} | Request: {Request}",
                requestName, userId, ipAddress, sanitizedRequest);
        }

        try
        {
            var response = await next();
            stopwatch.Stop();

            if (loggableRequest.LogResponse)
            {
                _logger.LogInformation(
                    "Completed {RequestName} | User: {UserId} | Duration: {Duration}ms | Response: {Response}",
                    requestName, userId, stopwatch.ElapsedMilliseconds, JsonSerializer.Serialize(response));
            }
            else
            {
                _logger.LogInformation(
                    "Completed {RequestName} | User: {UserId} | Duration: {Duration}ms",
                    requestName, userId, stopwatch.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Failed {RequestName} | User: {UserId} | IP: {IpAddress} | Duration: {Duration}ms | Error: {Error}",
                requestName, userId, ipAddress, stopwatch.ElapsedMilliseconds, ex.Message);
            throw;
        }
    }

    private string? GetUserId()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value
               ?? _httpContextAccessor.HttpContext?.User?.FindFirst("userId")?.Value;
    }

    private string? GetIpAddress()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return null;

        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }

    private static string SanitizeRequest(TRequest request)
    {
        var json = JsonSerializer.Serialize(request);
        var sensitiveFields = new[] { "password", "passwordHash", "token", "secret", "apiKey" };
        
        foreach (var field in sensitiveFields)
        {
            json = System.Text.RegularExpressions.Regex.Replace(
                json,
                $"\"{field}\"\\s*:\\s*\"[^\"]*\"",
                $"\"{field}\":\"[REDACTED]\"",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return json;
    }
}
