using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using MediatR;

namespace Identity.Application.Commands.RegisterMaintenanceCompany;

public class RegisterMaintenanceCompanyHandler : IRequestHandler<RegisterMaintenanceCompanyCommand, Guid>
{
    private readonly IUserRepository _userRepo;
    private readonly IPasswordHasher _hasher;
    private readonly IMaintenanceCompanyRepository _companyRepo;

    public RegisterMaintenanceCompanyHandler(
        IUserRepository userRepo,
        IPasswordHasher hasher,
        IMaintenanceCompanyRepository companyRepo)
    {
        _userRepo = userRepo;
        _hasher = hasher;
        _companyRepo = companyRepo;
    }

    public async Task<Guid> Handle(RegisterMaintenanceCompanyCommand request, CancellationToken cancellationToken)
    {
        if (await _userRepo.ExistsByEmailAsync(request.Email, cancellationToken))
            throw new InvalidOperationException("Email is already registered.");

        var user = User.Create(
            fullName: request.FullName,
            email: request.Email,
            passwordHash: _hasher.Hash(request.TempPassword),
            phone: request.Phone,
            role: UserRole.MaintenanceCompany);

        user.VerifyEmail(user.EmailVerificationToken!);
        await _userRepo.AddAsync(user, cancellationToken);

        // Mirror into identity.maintenance_companies so toggle/search handlers have data.
        // ServiceTypes and City are filled in later via the MaintenanceCompanies module.
        var identityRecord = MaintenanceCompany.Create(
            userId: user.Id,
            companyName: request.FullName,
            serviceTypes: Array.Empty<string>(),
            email: request.Email,
            phone: request.Phone,
            city: string.Empty);

        await _companyRepo.AddAsync(identityRecord, cancellationToken);

        // Both repos share IdentityDbContext in the same DI scope — one call saves both.
        await _userRepo.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}
