using Identity.Application.Interfaces;
using MediatR;
using RealEstate.Shared.Kernel.Shared.Contracts;

namespace Identity.Application.Commands.ReactivateBroker;

public class ReactivateBrokerHandler : IRequestHandler<ReactivateBrokerCommand>
{
    private readonly IBrokerRepository _brokerRepo;
    private readonly IUserRepository _userRepo;
    private readonly ISubscriptionLimitChecker _limitChecker;

    public ReactivateBrokerHandler(
        IBrokerRepository brokerRepo,
        IUserRepository userRepo,
        ISubscriptionLimitChecker limitChecker)
    {
        _brokerRepo = brokerRepo;
        _userRepo = userRepo;
        _limitChecker = limitChecker;
    }

    public async Task Handle(ReactivateBrokerCommand request, CancellationToken cancellationToken)
    {
        var broker = await _brokerRepo.GetByIdAsync(request.BrokerId, cancellationToken)
            ?? throw new KeyNotFoundException("Broker not found.");

        if (!request.IsAdmin && broker.CompanyId != request.RequestingCompanyId)
            throw new UnauthorizedAccessException("You can only reactivate brokers in your own company.");

        if (broker.IsActive) return; // already active — nothing to do

        // Reactivating fills a broker seat again — respect the plan limit.
        // Sync the counter to the real active count first so historical
        // drift (adds that were never decremented) can't block the seat.
        if (broker.CompanyId is not null)
        {
            var activeBrokers = (await _brokerRepo.GetByCompanyIdAsync(broker.CompanyId.Value, cancellationToken))
                .Count(b => b.IsActive);
            await _limitChecker.SyncBrokerUsageAsync(broker.CompanyId.Value, activeBrokers, cancellationToken);

            var (allowed, reason) = await _limitChecker.CheckBrokerLimitAsync(broker.CompanyId.Value, cancellationToken);
            if (!allowed) throw new InvalidOperationException(reason);
        }

        broker.Activate();
        await _brokerRepo.SaveChangesAsync(cancellationToken);

        if (broker.CompanyId is not null)
            await _limitChecker.IncrementBrokerUsageAsync(broker.CompanyId.Value, cancellationToken);

        // Unblock the broker's login.
        var user = await _userRepo.GetByIdAsync(broker.UserId, cancellationToken);
        if (user is not null)
        {
            user.Activate();
            await _userRepo.SaveChangesAsync(cancellationToken);
        }
    }
}
