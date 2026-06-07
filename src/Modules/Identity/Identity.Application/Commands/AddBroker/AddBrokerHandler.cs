using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using MediatR;
using RealEstate.Shared.Kernel.Shared.Contracts;

namespace Identity.Application.Commands.AddBroker;

public class AddBrokerHandler : IRequestHandler<AddBrokerCommand, Guid>
{
    private readonly IUserRepository _userRepo;
    private readonly IBrokerRepository _brokerRepo;
    private readonly IPasswordHasher _hasher;
    private readonly IEmailVerificationService _emailService;
    private readonly ISubscriptionLimitChecker _limitChecker;

    public AddBrokerHandler(
        IUserRepository userRepo,
        IBrokerRepository brokerRepo,
        IPasswordHasher hasher,
        IEmailVerificationService emailService,
        ISubscriptionLimitChecker limitChecker)
    {
        _userRepo = userRepo;
        _brokerRepo = brokerRepo;
        _hasher = hasher;
        _emailService = emailService;
        _limitChecker = limitChecker;
    }

    public async Task<Guid> Handle(AddBrokerCommand request, CancellationToken cancellationToken)
    {
        var (allowed, reason) = await _limitChecker.CheckBrokerLimitAsync(request.CompanyId, cancellationToken);
        if (!allowed)
            throw new InvalidOperationException(reason);

        if (await _userRepo.ExistsByEmailAsync(request.Email, cancellationToken))
            throw new InvalidOperationException("Email is already registered.");

        if (await _brokerRepo.ExistsByLicenseAsync(request.LicenseNumber, cancellationToken))
            throw new InvalidOperationException("Broker license is already registered.");

        var user = User.Create(
            fullName: request.FullName,
            email: request.Email,
            passwordHash: _hasher.Hash(request.TempPassword),
            phone: request.Phone,
            role: UserRole.Broker);
        user.SetCompany(request.CompanyId);

        await _userRepo.AddAsync(user, cancellationToken);
        await _userRepo.SaveChangesAsync(cancellationToken);

        var broker = Broker.Create(
            userId: user.Id,
            companyId: request.CompanyId,
            licenseNumber: request.LicenseNumber);

        await _brokerRepo.AddAsync(broker, cancellationToken);
        await _brokerRepo.SaveChangesAsync(cancellationToken);

        await _limitChecker.IncrementBrokerUsageAsync(request.CompanyId, cancellationToken);

        _ = _emailService.SendVerificationLinkAsync(user, CancellationToken.None);

        return broker.Id;
    }
}
