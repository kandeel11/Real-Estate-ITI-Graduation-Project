using AI.Application.Common.Behaviors;   // ← ValidationBehavior
using AI.Application.Features.AdminAnalytics.Plugins;
using AI.Application.Features.Chat.Commands;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AI.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAiApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var assembly = typeof(SendMessageCommand).Assembly;

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(assembly));

        services.AddValidatorsFromAssembly(assembly);

        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(ValidationBehavior<,>));

        // AdminAnalyticsPlugin depends only on Shared analytics interfaces.
        // The four IXxxAnalyticsService implementations are registered by their
        // respective modules (Property, Payment, Subscription, Identity).
        services.AddScoped<AdminAnalyticsPlugin>();

        return services;
    }
}