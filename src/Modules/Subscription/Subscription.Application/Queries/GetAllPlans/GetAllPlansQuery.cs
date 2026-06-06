using MediatR;
using Subscription.Application.DTOs;

namespace Subscription.Application.Queries.GetAllPlans;

public record GetAllPlansQuery : IRequest<List<SubscriptionPlanDto>>;
