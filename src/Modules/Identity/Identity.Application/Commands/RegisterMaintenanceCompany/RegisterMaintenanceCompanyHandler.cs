using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using MediatR;

namespace Identity.Application.Commands.RegisterMaintenanceCompany;

public class RegisterMaintenanceCompanyHandler : IRequestHandler<RegisterMaintenanceCompanyCommand, Guid>
{
    private readonly IUserRepository _userRepo;
    private readonly IPasswordHasher _hasher;

    public RegisterMaintenanceCompanyHandler(
        IUserRepository userRepo,
        IPasswordHasher hasher)
    {
        _userRepo = userRepo;
        _hasher = hasher;
    }

    public async Task<Guid> Handle(RegisterMaintenanceCompanyCommand request, CancellationToken cancellationToken)
    {
        // التأكد إن الإيميل مش متسجل قبل كده
        if (await _userRepo.ExistsByEmailAsync(request.Email, cancellationToken))
            throw new InvalidOperationException("Email is already registered.");

        // إنشاء الحساب وإعطائه صلاحية شركة الصيانة
        var user = User.Create(
            fullName: request.FullName,
            email: request.Email,
            passwordHash: _hasher.Hash(request.TempPassword),
            phone: request.Phone,
            role: UserRole.MaintenanceCompany);

        // تفعيل الحساب تلقائياً لأن الأدمن هو اللي عامله
        user.VerifyEmail(user.EmailVerificationToken!);

        await _userRepo.AddAsync(user, cancellationToken);
        await _userRepo.SaveChangesAsync(cancellationToken);

        // إرجاع الـ ID عشان الكونترولر يستخدمه
        return user.Id;
    }
}

