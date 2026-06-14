using MediatR;
using Subscription.Application.DTOs;

namespace Subscription.Application.Queries.GetPlanById;

public record GetPlanByIdQuery(Guid PlanId) : IRequest<SubscriptionPlanDto?>;
