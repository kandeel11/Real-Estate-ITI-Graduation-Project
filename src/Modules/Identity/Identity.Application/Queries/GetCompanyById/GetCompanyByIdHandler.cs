using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using MediatR;

namespace Identity.Application.Queries.GetCompanyById;

public class GetCompanyByIdHandler : IRequestHandler<GetCompanyByIdQuery, CompanyDto?>
{
    private readonly ICompanyRepository _companyRepo;

    public GetCompanyByIdHandler(ICompanyRepository companyRepo)
        => _companyRepo = companyRepo;

    public async Task<CompanyDto?> Handle(GetCompanyByIdQuery request, CancellationToken cancellationToken)
    {
        var company = await _companyRepo.GetByIdAsync(request.CompanyId, cancellationToken);
        if (company is null) return null;

        return new CompanyDto(
            company.Id,
            company.Name,
            company.CommercialRegister,
            company.City,
            company.Address,
            company.Phone,
            company.Email,
            company.IsVerified,
            company.RatingAvg,
            company.TotalRatings);
    }
}
