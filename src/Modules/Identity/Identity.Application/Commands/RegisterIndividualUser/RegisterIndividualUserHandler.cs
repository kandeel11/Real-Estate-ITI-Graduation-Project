using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;

namespace Identity.Application.Commands.RegisterIndividualUser;

public class RegisterIndividualUserHandler
    : IRequestHandler<RegisterIndividualUserCommand, Guid>
{
    private readonly IUserRepository _userRepo;
    private readonly IBrokerRepository _brokerRepo;
    private readonly IPasswordHasher _hasher;
    private readonly IEmailVerificationService _emailService;
    private readonly IPublisher _publisher;
    private readonly ILogger<RegisterIndividualUserHandler> _logger;

    public RegisterIndividualUserHandler(
        IUserRepository userRepo,
        IBrokerRepository brokerRepo,
        IPasswordHasher hasher,
        IEmailVerificationService emailService,
        IPublisher publisher,
        ILogger<RegisterIndividualUserHandler> logger)
    {
        _userRepo = userRepo;
        _brokerRepo = brokerRepo;
        _hasher = hasher;
        _emailService = emailService;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<Guid> Handle(RegisterIndividualUserCommand request, CancellationToken cancellationToken)
    {
        if (await _userRepo.ExistsByEmailAsync(request.Email, cancellationToken))
            throw new InvalidOperationException("Email is already registered.");

        if (!Enum.TryParse<UserRole>(request.Role, true, out var roleEnum))
            throw new InvalidOperationException($"Invalid role: {request.Role}. Valid values: Owner, Broker, Tenant.");

        if (roleEnum is not (UserRole.Owner or UserRole.Broker or UserRole.Tenant))
            throw new InvalidOperationException("Individual registration is only for Owner, Broker, or Tenant.");

        var user = User.Create(
            fullName: request.FullName,
            email: request.Email,
            passwordHash: _hasher.Hash(request.Password),
            phone: request.Phone,
            role: roleEnum,
            nationalIdNumber: request.NationalIdNumber);

        await _userRepo.AddAsync(user, cancellationToken);
        await _userRepo.SaveChangesAsync(cancellationToken);

        // Create Broker entity for individual brokers (CompanyId = null)
        if (roleEnum == UserRole.Broker)
        {
            // Auto-generate license number for demo (format: BROKER-{UserId})
            var licenseSuffix = user.Id.ToString("N").Substring(0, 8).ToUpper();
            var generatedLicense = $"DEMO-{licenseSuffix}";

            var broker = Broker.Create(
                userId: user.Id,
                companyId: null,  // Individual broker (no company)
                licenseNumber: generatedLicense);

            await _brokerRepo.AddAsync(broker, cancellationToken);
            await _brokerRepo.SaveChangesAsync(cancellationToken);
        }

        _ = _emailService.SendVerificationLinkAsync(user, CancellationToken.None);

        var accountType = roleEnum switch
        {
            UserRole.Owner => "Owner",
            UserRole.Broker => "Broker",
            UserRole.Tenant => "Tenant",
            _ => throw new InvalidOperationException()
        };

        try
        {
            await _publisher.Publish(
                new IndividualUserCreatedEvent(user.Id, accountType),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish IndividualUserCreatedEvent for UserId={UserId}", user.Id);
        }

        return user.Id;
    }
}
