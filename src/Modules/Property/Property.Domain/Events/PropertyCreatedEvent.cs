using MediatR;

namespace Property.Domain.Events
{
    public sealed record PropertyCreatedEvent(
     Guid PropertyId,
     Guid OwnerUserId,
     string OwnerName,
     string OwnerNationalIdBlobPath,
     string OwnershipProofBlobPath,
     string Country,
     string Governorate,
     string City,
     string? District,
     string AddressLine
 ) : INotification;
}
