using Identity.Application.Interfaces;
using MediatR;

namespace Identity.Application.Commands.UpdateCompanyLogo;

public class UpdateCompanyLogoHandler : IRequestHandler<UpdateCompanyLogoCommand>
{
    private readonly ICompanyRepository _companies;

    public UpdateCompanyLogoHandler(ICompanyRepository companies) => _companies = companies;

    public async Task Handle(UpdateCompanyLogoCommand request, CancellationToken cancellationToken)
    {
        var company = await _companies.GetByIdAsync(request.CompanyId, cancellationToken)
            ?? throw new KeyNotFoundException("Company not found.");

        company.SetLogo(request.LogoUrl);
        await _companies.SaveChangesAsync(cancellationToken);
    }
}
