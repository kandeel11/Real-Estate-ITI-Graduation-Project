using MediatR;

namespace AI.Application.Features.AdminAnalytics;

public sealed record AdminAnalyticsQuery(string Prompt) : IRequest<AdminAnalyticsResponse>;

public sealed record AdminAnalyticsResponse(string Answer);
