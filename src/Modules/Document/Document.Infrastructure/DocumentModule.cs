using Document.Application.Interfaces;
using Document.Infrastructure.Persistence;
using Document.Infrastructure.Repositories;
using Document.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RealEstate.Shared.Kernel.Interfaces;

namespace Document.Infrastructure;

public static class DocumentModule
{
    public static IServiceCollection AddDocumentModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<DocumentDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DocumentDb"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "document")));

        services.AddScoped<IInquiryRepository, InquiryRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationService, NotificationService>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(
                typeof(Application.Commands.Inquiry.CreateInquiryCommandHandler).Assembly));

        return services;
    }
}