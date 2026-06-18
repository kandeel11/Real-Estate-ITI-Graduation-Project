namespace Payment.Application.Interfaces;

/// <summary>
/// Unit of Work — يضمن حفظ كل التغييرات (Payment + SubscriptionPaymentDetails +
/// OutboxMessage) في معاملة واحدة (Single Transaction)
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
