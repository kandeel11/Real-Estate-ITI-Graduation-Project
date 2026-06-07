using MediatR;
using Subscription.Application.Interfaces;

namespace Subscription.Application.Commands.DeactivatePlan;

public class DeactivatePlanHandler : IRequestHandler<DeactivatePlanCommand>
{
    private readonly ISubscriptionPlanRepository _repository;

    public DeactivatePlanHandler(ISubscriptionPlanRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(DeactivatePlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await _repository.GetByIdAsync(request.PlanId, cancellationToken)
            ?? throw new KeyNotFoundException($"Subscription plan {request.PlanId} not found.");

        plan.Deactivate();
        await _repository.UpdateAsync(plan, cancellationToken);
    }
}
