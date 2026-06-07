using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;

namespace Identity.Application.Commands.RegisterCompany;

public class RegisterCompanyHandler
    : IRequestHandler<RegisterCompanyCommand, Guid>
{
    private readonly IUserRepository _userRepo;
    private readonly ICompanyRepository _companyRepo;
    private readonly IPasswordHasher _hasher;
    private readonly IEmailVerificationService _emailService;
    private readonly IPublisher _publisher;
    private readonly ILogger<RegisterCompanyHandler> _logger;

    public RegisterCompanyHandler(
        IUserRepository userRepo,
        ICompanyRepository companyRepo,
        IPasswordHasher hasher,
        IEmailVerificationService emailService,
        IPublisher publisher,
        ILogger<RegisterCompanyHandler> logger)
    {
        _userRepo = userRepo;
        _companyRepo = companyRepo;
        _hasher = hasher;
        _emailService = emailService;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<Guid> Handle(RegisterCompanyCommand request, CancellationToken cancellationToken)
    {
        if (await _userRepo.ExistsByEmailAsync(request.AdminEmail, cancellationToken))
            throw new InvalidOperationException("Admin email is already registered.");

        if (await _companyRepo.ExistsByCommercialRegisterAsync(request.CommercialRegister, cancellationToken))
            throw new InvalidOperationException("A company with this commercial register already exists.");

        var adminUser = User.Create(
            fullName: request.AdminFullName,
            email: request.AdminEmail,
            passwordHash: _hasher.Hash(request.Password),
            phone: request.AdminPhone,
            role: UserRole.CompanyManager);

        await _userRepo.AddAsync(adminUser, cancellationToken);
        await _userRepo.SaveChangesAsync(cancellationToken);

        var company = Company.Create(
            managerId: adminUser.Id,
            name: request.CompanyName,
            commercialRegister: request.CommercialRegister,
            city: request.City,
            address: request.Address,
            phone: request.CompanyPhone,
            email: request.CompanyEmail);

        await _companyRepo.AddAsync(company, cancellationToken);

        adminUser.SetCompany(company.Id);
        await _userRepo.SaveChangesAsync(cancellationToken);
        await _companyRepo.SaveChangesAsync(cancellationToken);

        _ = _emailService.SendVerificationEmailAsync(adminUser, CancellationToken.None);

        try
        {
            await _publisher.Publish(
                new CompanyCreatedEvent(company.Id, adminUser.Id),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish CompanyCreatedEvent for CompanyId={CompanyId}", company.Id);
        }

        return company.Id;
    }
}
