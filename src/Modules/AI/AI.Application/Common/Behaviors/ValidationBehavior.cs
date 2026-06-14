// AI.Application/Common/Behaviors/ValidationBehavior.cs

using FluentValidation;
using MediatR;

namespace AI.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that automatically validates every
/// Command/Query before it reaches its handler.
/// Throws ValidationException if any rule fails.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // No validators registered for this request → skip
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        // Run all validators and collect failures
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count > 0)
            throw new ValidationException(failures);

        // All rules passed → continue to handler
        return await next();
    }
}