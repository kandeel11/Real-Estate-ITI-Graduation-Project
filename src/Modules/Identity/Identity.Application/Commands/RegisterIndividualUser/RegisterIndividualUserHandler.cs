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
    private readonly IPasswordHasher _hasher;
    private readonly IEmailVerificationService _emailService;
    private readonly IPublisher _publisher;
    private readonly ILogger<RegisterIndividualUserHandler> _logger;

    public RegisterIndividualUserHandler(
        IUserRepository userRepo,
        IPasswordHasher hasher,
        IEmailVerificationService emailService,
        IPublisher publisher,
        ILogger<RegisterIndividualUserHandler> logger)
    {
        _userRepo = userRepo;
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
