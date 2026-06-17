using Identity.Application.Interfaces;
using MaintenanceCompanies.Application.DTOs.Categories;
using MaintenanceCompanies.Application.DTOs.Companies;
using MaintenanceCompanies.Application.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace MaintenanceCompanies.Application.Queries.GetMyCompany
{
   public record GetMyCompanyQuery : IRequest<MaintenanceCompanyResponseDto>
    {
    }

    public class GetMyCompanyQueryHandler : IRequestHandler<GetMyCompanyQuery, MaintenanceCompanyResponseDto>
    {
        private readonly IMaintenanceCompanyService _repository;
        private readonly ICurrentUserService _currentUserService;
        public GetMyCompanyQueryHandler(IMaintenanceCompanyService repository, ICurrentUserService currentUserService)
        {
            _repository = repository;
            _currentUserService = currentUserService;
        }
        public async Task<MaintenanceCompanyResponseDto> Handle(GetMyCompanyQuery request, CancellationToken cancellationToken)
        {
            var company = await _repository.GetByUserIdAsync(_currentUserService.UserId.ToString());
            if (company == null)
            {
                throw new Exception ($"No maintenance company found for user ID {_currentUserService.UserId.ToString()}");
            }
            return new MaintenanceCompanyResponseDto(
                company.Id,
                company.CompanyName,
                company.Email,
                company.Phone,
                company.City,
                company.ServiceRadiusKm,
                company.LogoUrl,
                company.Description,
                company.IsActive,
                company.AverageRating,
                company.CompletedTicketsCount,
                company.ActiveTicketsCount,
                company.CreatedAt,
                company.Categories.Select(c => new CategoryDto (c.Id, c.Name,c.Icon,c.Description,c.IsActive)).ToList()
            );
        }
    }

}
