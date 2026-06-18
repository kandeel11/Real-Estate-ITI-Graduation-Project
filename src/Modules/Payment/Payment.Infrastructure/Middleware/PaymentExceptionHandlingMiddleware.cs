using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Payment.Application.Exceptions;

namespace Payment.Infrastructure.Middleware;

/// <summary>
/// Global Exception Handling Middleware بتاع Payment Module
///
/// بيحول كل exception لـ structured JSON response بدل صفحة HTML الافتراضية
///
/// Mapping:
/// PaymentNotFoundException         → 404 Not Found
/// InvalidPaymentStateException     → 422 Unprocessable Entity
/// DuplicatePaymentException        → 409 Conflict
/// PaymentGatewayException          → 402 Payment Required
/// ValidationException (Fluent)     → 400 Bad Request
/// أي exception تاني                → 500 Internal Server Error
///
/// كيفية الاستخدام في Program.cs:
///     app.UseMiddleware&lt;PaymentExceptionHandlingMiddleware&gt;();
/// </summary>
public sealed class PaymentExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PaymentExceptionHandlingMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public PaymentExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<PaymentExceptionHandlingMiddleware> logger)
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
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, errorResponse) = exception switch
        {
            PaymentNotFoundException ex => (
                HttpStatusCode.NotFound,
                new ErrorResponse("PAYMENT_NOT_FOUND", ex.Message)
            ),

            PlanNotFoundException ex => (
                HttpStatusCode.NotFound,
                new ErrorResponse("PLAN_NOT_FOUND", ex.Message)
            ),

            InvalidPaymentStateException ex => (
                HttpStatusCode.UnprocessableEntity,
                new ErrorResponse("INVALID_PAYMENT_STATE", ex.Message,
                    new { ex.CurrentStatus, ex.RequiredStatus })
            ),

            DuplicatePaymentException ex => (
                HttpStatusCode.Conflict,
                new ErrorResponse("DUPLICATE_PAYMENT", ex.Message)
            ),

            PaymentGatewayException ex => (
                HttpStatusCode.PaymentRequired,
                new ErrorResponse("PAYMENT_GATEWAY_ERROR", ex.Message,
                    new { GatewayCode = ex.GatewayCode })
            ),

            ValidationException ex => (
                HttpStatusCode.BadRequest,
                new ErrorResponse("VALIDATION_ERROR", "One or more validation errors occurred.",
                    ex.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray()))
            ),

            OperationCanceledException => (
                HttpStatusCode.BadRequest,
                new ErrorResponse("REQUEST_CANCELLED", "The request was cancelled.")
            ),

            _ => (
                HttpStatusCode.InternalServerError,
                new ErrorResponse("INTERNAL_ERROR", "An unexpected error occurred.")
            )
        };

        // Log بمستوى مناسب
        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Unhandled exception for {Path}", context.Request.Path);
        else
            _logger.LogWarning(exception, "Handled exception: {Type} for {Path}",
                exception.GetType().Name, context.Request.Path);

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var json = JsonSerializer.Serialize(errorResponse, JsonOptions);
        await context.Response.WriteAsync(json);
    }
}

/// <summary>
/// الـ Shape الموحد لكل error responses في الـ Payment module
/// </summary>
public sealed record ErrorResponse(
    string Code,
    string Message,
    object? Details = null
);
