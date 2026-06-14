// Property.Application/Features/Commands/CreateProperty/CreatePropertyCommand.cs

using MediatR;
using Property.Domain.Enums;

namespace Property.Application.Features.Commands.CreateProperty;

public sealed record CreatePropertyCommand(
    Guid OwnerId,
    Guid? CompanyId,
    Guid? ListingBrokerId,
    string Title,
    string TitleAr,
    string Description,
    string DescriptionAr,
    PropertyType PropertyType,
    decimal Price,
    int Bedrooms,
    int Bathrooms,
    decimal AreaSqm,
    int? FloorNumber,
    int? TotalFloors,
    int? YearBuilt,
    bool IsFurnished,
    bool PetsAllowed,
    string Address,
    int? ApartmentNumber,
    string Country,
    string Governorate,
    string ownerShipStatus, // e.g. "verified", "pending", "rejected"
    string City,
    string? District,
    ListingSource ListingSource,

    // 🆕 Ownership docs — null when submitted by a broker
    bool IsOwnerSubmission,
    string? OwnerFullName,
    string? NationalIdBlobPath,
    string? OwnershipProofBlobPath
) : IRequest<Guid>;