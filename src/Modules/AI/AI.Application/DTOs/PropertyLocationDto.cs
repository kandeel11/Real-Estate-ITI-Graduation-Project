// AI.Application/Features/OwnershipVerification/DTOs/PropertyLocationDto.cs
namespace AI.Application.Features.OwnershipVerification.DTOs;

public sealed record PropertyLocationDto(
    string Country,
    string Governorate,    // محافظة
    string City,
    string? District,       // الحي
    string AddressLine);