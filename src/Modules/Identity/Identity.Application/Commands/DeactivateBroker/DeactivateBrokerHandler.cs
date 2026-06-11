using Identity.Application.Interfaces;
using MediatR;
using RealEstate.Shared.Kernel.Shared.Contracts;

namespace Identity.Application.Commands.DeactivateBroker;

public class DeactivateBrokerHandler : IRequestHandler<DeactivateBrokerCommand>
{
    private readonly IBrokerRepository _brokerRepo;
    private readonly IUserRepository _userRepo;
    private readonly ISubscriptionLimitChecker _limitChecker;

    public DeactivateBrokerHandler(
        IBrokerRepository brokerRepo,
        IUserRepository userRepo,
        ISubscriptionLimitChecker limitChecker)
    {
        _brokerRepo = brokerRepo;
        _userRepo = userRepo;
        _limitChecker = limitChecker;
    }

    public async Task Handle(DeactivateBrokerCommand request, CancellationToken cancellationToken)
    {
        var broker = await _brokerRepo.GetByIdAsync(request.BrokerId, cancellationToken)
            ?? throw new KeyNotFoundException("Broker not found.");

        // A company manager can only deactivate brokers in their own company; admins, any.
        if (!request.IsAdmin && broker.CompanyId != request.RequestingCompanyId)
            throw new UnauthorizedAccessException("You can only deactivate brokers in your own company.");

        broker.Deactivate();
        await _brokerRepo.SaveChangesAsync(cancellationToken);

        // Free the plan seat: realign the usage counter with actual actives
        if (broker.CompanyId is not null)
        {
            var activeBrokers = (await _brokerRepo.GetByCompanyIdAsync(broker.CompanyId.Value, cancellationToken))
                .Count(b => b.IsActive);
            await _limitChecker.SyncBrokerUsageAsync(broker.CompanyId.Value, activeBrokers, cancellationToken);
        }

        // Also block the broker's login.
        var user = await _userRepo.GetByIdAsync(broker.UserId, cancellationToken);
        if (user is not null)
        {
            user.Deactivate();
            await _userRepo.SaveChangesAsync(cancellationToken);
        }
    }
}
