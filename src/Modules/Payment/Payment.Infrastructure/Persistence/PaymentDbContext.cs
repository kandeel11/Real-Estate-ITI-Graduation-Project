using Microsoft.EntityFrameworkCore;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.Persistence;

public sealed class PaymentDbContext : DbContext, IPaymentDbContext{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
    {
    }

    public DbSet<RentPayment> RentPayments => Set<RentPayment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaymentDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
