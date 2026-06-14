using MediatR;
using Property.Application.DTOs;

namespace Property.Application.Features.Queries.GetPropertyAnalytics;

public record GetPropertyAnalyticsQuery(Guid PropertyId, Guid RequestingUserId) : IRequest<AnalyticsDto>;
