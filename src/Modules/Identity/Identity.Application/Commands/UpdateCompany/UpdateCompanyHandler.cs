using Identity.Application.Interfaces;
using MediatR;

namespace Identity.Application.Commands.UpdateCompany;

public class UpdateCompanyHandler : IRequestHandler<UpdateCompanyCommand>
{
    private readonly ICompanyRepository _companyRepo;

    public UpdateCompanyHandler(ICompanyRepository companyRepo)
        => _companyRepo = companyRepo;

    public async Task Handle(UpdateCompanyCommand request, CancellationToken cancellationToken)
    {
        var company = await _companyRepo.GetByIdAsync(request.CompanyId, cancellationToken)
            ?? throw new InvalidOperationException("Company not found.");

        if (company.ManagerId != request.RequestingUserId)
            throw new UnauthorizedAccessException("Only the company manager can update company details.");

        company.UpdateDetails(
            request.Name,
            request.City,
            request.Address,
            request.Phone,
            request.Website,
            request.Description,
            request.LogoUrl);

        await _companyRepo.SaveChangesAsync(cancellationToken);
    }
}
