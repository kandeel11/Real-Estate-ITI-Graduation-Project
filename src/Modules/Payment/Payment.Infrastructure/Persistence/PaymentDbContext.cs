using Microsoft.EntityFrameworkCore;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.Persistence;

/// <summary>
/// PaymentDbContext — Payment module هو single source of truth
/// لكل المعاملات المالية في النظام
/// </summary>
public sealed class PaymentDbContext : DbContext, IPaymentDbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
    {
    }

    public DbSet<Payment.Domain.Entities.Payment> Payments => Set<Payment.Domain.Entities.Payment>();

    public DbSet<RentPaymentDetails> RentPaymentDetails => Set<RentPaymentDetails>();

    public DbSet<SubscriptionPaymentDetails> SubscriptionPaymentDetails => Set<SubscriptionPaymentDetails>();

    public DbSet<TransferPaymentDetails> TransferPaymentDetails => Set<TransferPaymentDetails>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaymentDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
