using Microsoft.Extensions.Logging;
using Payment.Application.Interfaces;

namespace Payment.Infrastructure.Persistence;

/// <summary>
/// Implementation بتاع IUnitOfWork
/// بيستخدم PaymentDbContext.SaveChangesAsync — بيضمن إن كل التغييرات
/// (Payment + SubscriptionPaymentDetails + OutboxMessage) تُحفظ في معاملة واحدة
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly PaymentDbContext _context;
    private readonly ILogger<UnitOfWork> _logger;

    public UnitOfWork(PaymentDbContext context, ILogger<UnitOfWork> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UnitOfWork.SaveChangesAsync failed.");
            throw;
        }
    }
}
