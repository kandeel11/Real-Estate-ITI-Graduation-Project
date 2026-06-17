using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using MediatR;

namespace Identity.Application.Queries.GetAllCompanies;

public class GetAllCompaniesHandler : IRequestHandler<GetAllCompaniesQuery, List<CompanyDetailsDto>>
{
    private readonly ICompanyRepository _companyRepo;

    public GetAllCompaniesHandler(ICompanyRepository companyRepo)
        => _companyRepo = companyRepo;

    public async Task<List<CompanyDetailsDto>> Handle(
        GetAllCompaniesQuery request, CancellationToken cancellationToken)
    {
        var companies = await _companyRepo.GetAllAsync(cancellationToken);

        return companies.Select(c => new CompanyDetailsDto(
            c.Id,
            c.ManagerId,
            c.Name,
            c.CommercialRegister,
            c.LogoUrl,
            c.Website,
            c.Description,
            c.City,
            c.Address,
            c.Phone,
            c.Email,
            c.IsVerified,
            c.IsActive,
            c.RatingAvg,
            c.TotalRatings,
            c.CreatedAt)).ToList();
    }
}
