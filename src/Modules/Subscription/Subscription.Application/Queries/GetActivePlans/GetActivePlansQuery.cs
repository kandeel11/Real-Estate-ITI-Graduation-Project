using MediatR;
using Subscription.Application.DTOs;

namespace Subscription.Application.Queries.GetActivePlans;

public record GetActivePlansQuery : IRequest<List<SubscriptionPlanDto>>;
