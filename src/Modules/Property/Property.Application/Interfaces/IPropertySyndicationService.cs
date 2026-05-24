namespace Property.Application.Interfaces;

public interface IPropertySyndicationService
{
    Task SyndicateAsync(Guid propertyId, CancellationToken ct = default);
}
