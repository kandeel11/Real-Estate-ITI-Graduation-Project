using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using MediatR;

namespace Identity.Application.Queries.GetBrokersByCompany;

public class GetBrokersByCompanyHandler : IRequestHandler<GetBrokersByCompanyQuery, List<BrokerDto>>
{
    private readonly IBrokerRepository _brokerRepo;

    public GetBrokersByCompanyHandler(IBrokerRepository brokerRepo)
        => _brokerRepo = brokerRepo;

    public async Task<List<BrokerDto>> Handle(GetBrokersByCompanyQuery request, CancellationToken cancellationToken)
    {
        var brokers = await _brokerRepo.GetByCompanyIdAsync(request.CompanyId, cancellationToken);

        return brokers.Select(b => new BrokerDto(
            b.Id,
            b.UserId,
            b.CompanyId,
            b.LicenseNumber,
            b.IsActive,
            b.AiPerformanceScore,
            b.RatingAvg,
            b.TotalRatings)).ToList();
    }
}
