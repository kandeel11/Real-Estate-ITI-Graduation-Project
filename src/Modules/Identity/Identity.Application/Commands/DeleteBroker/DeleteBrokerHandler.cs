using Identity.Application.Interfaces;
using MediatR;
using RealEstate.Shared.Kernel.Shared.Contracts;

namespace Identity.Application.Commands.DeleteBroker;

public class DeleteBrokerHandler : IRequestHandler<DeleteBrokerCommand>
{
    private readonly IBrokerRepository _brokerRepo;
    private readonly IUserRepository _userRepo;
    private readonly ISubscriptionLimitChecker _limitChecker;

    public DeleteBrokerHandler(
        IBrokerRepository brokerRepo,
        IUserRepository userRepo,
        ISubscriptionLimitChecker limitChecker)
    {
        _brokerRepo = brokerRepo;
        _userRepo = userRepo;
        _limitChecker = limitChecker;
    }

    public async Task Handle(DeleteBrokerCommand request, CancellationToken cancellationToken)
    {
        var broker = await _brokerRepo.GetByIdAsync(request.BrokerId, cancellationToken)
            ?? throw new KeyNotFoundException("Broker not found.");

        if (!request.IsAdmin && broker.CompanyId != request.RequestingCompanyId)
            throw new UnauthorizedAccessException("You can only delete brokers in your own company.");

        // The user account stays (audit trail / FK targets like ListingBrokerId)
        // but can no longer sign in.
        var user = await _userRepo.GetByIdAsync(broker.UserId, cancellationToken);
        if (user is not null)
        {
            user.Deactivate();
        }

        var companyId = broker.CompanyId;

        _brokerRepo.Remove(broker);
        await _brokerRepo.SaveChangesAsync(cancellationToken);

        if (user is not null)
        {
            await _userRepo.SaveChangesAsync(cancellationToken);
        }

        // Free the plan seat: realign the usage counter with actual actives
        if (companyId is not null)
        {
            var activeBrokers = (await _brokerRepo.GetByCompanyIdAsync(companyId.Value, cancellationToken))
                .Count(b => b.IsActive);
            await _limitChecker.SyncBrokerUsageAsync(companyId.Value, activeBrokers, cancellationToken);
        }
    }
}
