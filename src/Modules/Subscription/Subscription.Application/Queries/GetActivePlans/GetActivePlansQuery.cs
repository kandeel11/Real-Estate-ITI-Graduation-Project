using MediatR;
using Subscription.Application.DTOs;

namespace Subscription.Application.Queries.GetActivePlans;

public record GetActivePlansQuery(string? Audience = null) : IRequest<List<SubscriptionPlanDto>>;
