using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using MediatR;

namespace Identity.Application.Queries.GetCompanyBrokersDetails;

public class GetCompanyBrokersDetailsHandler
    : IRequestHandler<GetCompanyBrokersDetailsQuery, List<BrokerDetailsDto>>
{
    private readonly IBrokerRepository _brokerRepo;
    private readonly IUserRepository _userRepo;

    public GetCompanyBrokersDetailsHandler(IBrokerRepository brokerRepo, IUserRepository userRepo)
    {
        _brokerRepo = brokerRepo;
        _userRepo = userRepo;
    }

    public async Task<List<BrokerDetailsDto>> Handle(
        GetCompanyBrokersDetailsQuery request, CancellationToken cancellationToken)
    {
        var brokers = await _brokerRepo.GetByCompanyIdAsync(request.CompanyId, cancellationToken);

        var result = new List<BrokerDetailsDto>(brokers.Count);
        foreach (var broker in brokers)
        {
            // Per-broker lookup is fine here: companies cap out at 25 brokers
            var user = await _userRepo.GetByIdAsync(broker.UserId, cancellationToken);

            result.Add(new BrokerDetailsDto(
                broker.Id,
                broker.UserId,
                broker.CompanyId,
                user?.FullName ?? string.Empty,
                user?.Email ?? string.Empty,
                user?.Phone ?? string.Empty,
                user?.ProfileImageUrl,
                broker.LicenseNumber,
                broker.IsActive,
                broker.AiPerformanceScore,
                broker.ClosedDealsCount,
                broker.AvgResponseTimeMinutes,
                broker.ConversionRate,
                broker.RatingAvg,
                broker.TotalRatings,
                broker.CreatedAt));
        }

        return result;
    }
}
