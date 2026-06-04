using MediatR;

namespace Subscription.Application.Commands.DeactivatePlan;

public record DeactivatePlanCommand(Guid PlanId) : IRequest;
