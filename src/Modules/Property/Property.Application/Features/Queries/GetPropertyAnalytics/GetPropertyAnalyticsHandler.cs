using MediatR;
using Property.Application.DTOs;
using Property.Application.Interfaces;

namespace Property.Application.Features.Queries.GetPropertyAnalytics;

public class GetPropertyAnalyticsHandler : IRequestHandler<GetPropertyAnalyticsQuery, AnalyticsDto>
{
    private readonly IPropertyRepository _propertyRepository;
    private readonly IPropertyAnalyticsRepository _analyticsRepository;

    public GetPropertyAnalyticsHandler(
        IPropertyRepository propertyRepository,
        IPropertyAnalyticsRepository analyticsRepository)
    {
        _propertyRepository = propertyRepository;
        _analyticsRepository = analyticsRepository;
    }

    public async Task<AnalyticsDto> Handle(GetPropertyAnalyticsQuery request, CancellationToken cancellationToken)
    {
        var property = await _propertyRepository.GetByIdAsync(request.PropertyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Property {request.PropertyId} not found.");

        if (property.OwnerId != request.RequestingUserId && property.CompanyId != request.RequestingUserId)
            throw new UnauthorizedAccessException("Not authorized to view analytics for this property.");

        return await _analyticsRepository.GetAnalyticsAsync(request.PropertyId, cancellationToken);
    }
}
