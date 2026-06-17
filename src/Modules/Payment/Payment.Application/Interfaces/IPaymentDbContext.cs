using Microsoft.EntityFrameworkCore;
using Payment.Domain.Entities;

namespace Payment.Application.Interfaces;

/// <summary>
/// الـ interface بتاع الـ DbContext بتاعنا
/// Application layer بيتعامل مع الـ interface مش الـ concrete class
/// </summary>
public interface IPaymentDbContext
{
    DbSet<Payment.Domain.Entities.Payment> Payments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
