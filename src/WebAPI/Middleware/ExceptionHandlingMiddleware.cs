using System.Net;
using System.Text.Json;
using FluentValidation;
using Mootable.Application.Pipelines.Authorization;
using Mootable.Domain.Exceptions;

namespace Mootable.WebAPI.Middleware;

/// <summary>
/// Global exception handling middleware.
/// 
/// NEDEN GLOBAL MIDDLEWARE:
/// 1. Tüm exception'lar tek noktadan geçer
/// 2. Consistent error response format
/// 3. Sensitive bilgi (stack trace) production'da gizlenir
/// 4. Logging merkezi yapılır
/// 
/// ANTI-PATTERN:
/// Her controller'da try-catch bloğu.
/// Bir developer unutur, 500 Internal Server Error + stack trace leak olur.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, response) = exception switch
        {
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                new ErrorResponse(
                    "Validation Failed",
                    validationEx.Errors.Select(e => new ErrorDetail(e.PropertyName, e.ErrorMessage)).ToList()
                )
            ),
            
            AuthorizationException authEx => (
                HttpStatusCode.Forbidden,
                new ErrorResponse(authEx.Message)
            ),
            
            BusinessRuleException businessEx => (
                HttpStatusCode.BadRequest,
                new ErrorResponse(businessEx.Message, businessEx.RuleCode)
            ),
            
            EntityNotFoundException notFoundEx => (
                HttpStatusCode.NotFound,
                new ErrorResponse(notFoundEx.Message)
            ),
            
            _ => (
                HttpStatusCode.InternalServerError,
                new ErrorResponse(
                    _environment.IsDevelopment() 
                        ? exception.Message 
                        : "An unexpected error occurred."
                )
            )
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);
        }
        else
        {
            _logger.LogWarning("Handled exception: {ExceptionType} - {Message}", 
                exception.GetType().Name, exception.Message);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}

public sealed record ErrorResponse(
    string Message,
    string? Code = null,
    IReadOnlyList<ErrorDetail>? Errors = null
)
{
    public ErrorResponse(string message, IReadOnlyList<ErrorDetail> errors) 
        : this(message, null, errors) { }
}

public sealed record ErrorDetail(string Field, string Message);
