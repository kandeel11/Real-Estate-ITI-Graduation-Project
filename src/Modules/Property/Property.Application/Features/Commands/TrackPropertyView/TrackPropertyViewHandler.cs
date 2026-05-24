using MediatR;
using Property.Application.Interfaces;
using Property.Domain.Entities;

namespace Property.Application.Features.Commands.TrackPropertyView;

public class TrackPropertyViewHandler : IRequestHandler<TrackPropertyViewCommand>
{
    private readonly IPropertyRepository _repository;
    private readonly IPropertyAnalyticsRepository _analyticsRepository;

    public TrackPropertyViewHandler(
        IPropertyRepository repository,
        IPropertyAnalyticsRepository analyticsRepository)
    {
        _repository = repository;
        _analyticsRepository = analyticsRepository;
    }

    public async Task Handle(TrackPropertyViewCommand request, CancellationToken cancellationToken)
    {
        var property = await _repository.GetByIdAsync(request.PropertyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Property {request.PropertyId} not found.");

        var analytics = PropertyAnalytics.Create(
            request.PropertyId,
            request.ViewerId,
            request.IpAddress,
            request.Source);

        await _analyticsRepository.AddAsync(analytics, cancellationToken);

        property.IncrementViews();

        await _repository.SaveChangesAsync(cancellationToken);
        await _analyticsRepository.SaveChangesAsync(cancellationToken);
    }
}
