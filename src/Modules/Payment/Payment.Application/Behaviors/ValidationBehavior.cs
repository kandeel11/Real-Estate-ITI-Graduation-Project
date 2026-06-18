using FluentValidation;
using MediatR;

namespace Payment.Application.Behaviors;

/// <summary>
/// MediatR Pipeline Behavior بيعمل validation تلقائياً قبل ما أي Command أو Query يوصل للـ Handler
/// 
/// إزاي بيشتغل:
/// Request → ValidationBehavior → Handler → Response
/// 
/// لو فيه validator للـ request ده، بيشتغل أول وبيرمي exception لو في errors
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // لو مفيش validators للـ request ده — اعدي مباشرة للـ Handler
        if (!_validators.Any())
            return await next();

        // شغّل كل الـ validators
        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next();
    }
}
