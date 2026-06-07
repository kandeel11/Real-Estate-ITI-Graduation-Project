using Identity.Application.Interfaces;
using MediatR;

namespace Identity.Application.Commands.VerifyCompany;

public class VerifyCompanyHandler : IRequestHandler<VerifyCompanyCommand>
{
    private readonly ICompanyRepository _companyRepo;

    public VerifyCompanyHandler(ICompanyRepository companyRepo)
        => _companyRepo = companyRepo;

    public async Task Handle(VerifyCompanyCommand request, CancellationToken cancellationToken)
    {
        var company = await _companyRepo.GetByIdAsync(request.CompanyId, cancellationToken)
            ?? throw new InvalidOperationException("Company not found.");

        if (company.IsVerified)
            throw new InvalidOperationException("Company is already verified.");

        company.Verify();
        await _companyRepo.SaveChangesAsync(cancellationToken);
    }
}
