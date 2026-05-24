using Property.Application.Interfaces;

namespace Property.Infrastructure.Services;

public class NullPropertySyndicationService : IPropertySyndicationService
{
    public Task SyndicateAsync(Guid propertyId, CancellationToken ct = default)
        => throw new InvalidOperationException("Zillow syndication is not configured. Set 'Zillow:BaseUrl' in appsettings.");
}
