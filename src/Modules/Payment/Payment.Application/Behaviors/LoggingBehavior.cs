using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Payment.Application.Behaviors;

/// <summary>
/// MediatR Pipeline Behavior بيعمل log لكل Command/Query
/// بيسجل:
/// - اسم الـ Request
/// - وقت التنفيذ بالـ milliseconds
/// - تحذير لو الـ request اخد أكتر من 500ms
///
/// ترتيب الـ Pipeline:
/// Request → LoggingBehavior → ValidationBehavior → Handler → Response
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("[Payment] Handling {RequestName}", requestName);

        try
        {
            var response = await next();

            stopwatch.Stop();

            // لو اتأخر أكتر من 500ms → warning
            if (stopwatch.ElapsedMilliseconds > 500)
            {
                _logger.LogWarning(
                    "[Payment] Slow request detected: {RequestName} took {ElapsedMs}ms",
                    requestName, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogInformation(
                    "[Payment] {RequestName} completed in {ElapsedMs}ms",
                    requestName, stopwatch.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "[Payment] {RequestName} failed after {ElapsedMs}ms",
                requestName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
