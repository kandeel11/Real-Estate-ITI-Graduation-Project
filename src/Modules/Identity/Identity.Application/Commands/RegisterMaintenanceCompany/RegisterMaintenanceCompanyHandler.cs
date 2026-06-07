using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using MediatR;

namespace Identity.Application.Commands.RegisterMaintenanceCompany;

public class RegisterMaintenanceCompanyHandler : IRequestHandler<RegisterMaintenanceCompanyCommand, Guid>
{
    private readonly IUserRepository _userRepo;
    private readonly IMaintenanceCompanyRepository _maintenanceRepo;
    private readonly IPasswordHasher _hasher;
    private readonly IEmailVerificationService _emailService;

    public RegisterMaintenanceCompanyHandler(
        IUserRepository userRepo,
        IMaintenanceCompanyRepository maintenanceRepo,
        IPasswordHasher hasher,
        IEmailVerificationService emailService)
    {
        _userRepo = userRepo;
        _maintenanceRepo = maintenanceRepo;
        _hasher = hasher;
        _emailService = emailService;
    }

    public async Task<Guid> Handle(RegisterMaintenanceCompanyCommand request, CancellationToken cancellationToken)
    {
        if (await _userRepo.ExistsByEmailAsync(request.Email, cancellationToken))
            throw new InvalidOperationException("Email is already registered.");

        if (await _maintenanceRepo.ExistsByEmailAsync(request.Email, cancellationToken))
            throw new InvalidOperationException("A maintenance company with this email already exists.");

        // Create user account with maintenance_company role
        var user = User.Create(
            fullName: request.FullName,
            email: request.Email,
            passwordHash: _hasher.Hash(request.TempPassword),
            phone: request.Phone,
            role: UserRole.MaintenanceCompany);

        // Admin-created accounts are auto-verified — no email verification needed
        user.VerifyEmail(user.EmailVerificationToken!);

        await _userRepo.AddAsync(user, cancellationToken);
        await _userRepo.SaveChangesAsync(cancellationToken);

        var company = MaintenanceCompany.Create(
            userId: user.Id,
            companyName: request.CompanyName,
            serviceTypes: request.ServiceTypes,
            email: request.Email,
            phone: request.Phone,
            city: request.City);

        await _maintenanceRepo.AddAsync(company, cancellationToken);
        await _maintenanceRepo.SaveChangesAsync(cancellationToken);

        return company.Id;
    }
}
