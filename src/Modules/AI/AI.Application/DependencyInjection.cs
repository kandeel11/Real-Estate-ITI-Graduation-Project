using AI.Application.Common.Behaviors;   // ← ValidationBehavior
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

        services.AddValidatorsFromAssembly(assembly);   // ✅ needs FluentValidation.DependencyInjectionExtensions

        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(ValidationBehavior<,>));              // ✅ needs AI.Application.Common.Behaviors

        return services;
    }
}