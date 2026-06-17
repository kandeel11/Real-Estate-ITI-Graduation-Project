using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using MediatR;

namespace Identity.Application.Queries.GetCompanyDetails;

public class GetCompanyDetailsHandler : IRequestHandler<GetCompanyDetailsQuery, CompanyDetailsDto?>
{
    private readonly ICompanyRepository _companyRepo;

    public GetCompanyDetailsHandler(ICompanyRepository companyRepo)
        => _companyRepo = companyRepo;

    public async Task<CompanyDetailsDto?> Handle(
        GetCompanyDetailsQuery request, CancellationToken cancellationToken)
    {
        var company = await _companyRepo.GetByIdAsync(request.CompanyId, cancellationToken);
        if (company is null) return null;

        return new CompanyDetailsDto(
            company.Id,
            company.ManagerId,
            company.Name,
            company.CommercialRegister,
            company.LogoUrl,
            company.Website,
            company.Description,
            company.City,
            company.Address,
            company.Phone,
            company.Email,
            company.IsVerified,
            company.IsActive,
            company.RatingAvg,
            company.TotalRatings,
            company.CreatedAt);
    }
}
