using MediatR;

namespace RealEstate.Shared.Kernel.Shared.IntegrationEvents
{
    public record PropertyDescriptionAcceptedEvent(Guid PropertyId,
    string DescriptionEn,
    string DescriptionAr) : INotification;

}
