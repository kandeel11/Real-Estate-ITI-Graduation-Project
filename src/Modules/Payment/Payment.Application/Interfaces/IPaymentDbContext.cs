using Microsoft.EntityFrameworkCore;
using Payment.Domain.Entities;

namespace Payment.Application.Interfaces;

public interface IPaymentDbContext
{
    DbSet<RentPayment> RentPayments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}