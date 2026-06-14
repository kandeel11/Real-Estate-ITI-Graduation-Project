// AI.Application/Features/OwnershipVerification/DTOs/LocationMatchDetails.cs

namespace AI.Application.Features.OwnershipVerification.DTOs;

public sealed class LocationMatchDetails
{
    public decimal CountryScore { get; set; }
    public decimal GovernorateScore { get; set; }
    public decimal CityScore { get; set; }
    public decimal DistrictScore { get; set; }
    public decimal AddressScore { get; set; }
    public string? Notes { get; set; }
}

public static class LocationMatchExtensions
{
    public static decimal AverageScore(this LocationMatchDetails d)
    {
        var weighted =
            d.CountryScore * 0.10m +
            d.GovernorateScore * 0.25m +
            d.CityScore * 0.30m +
            d.DistrictScore * 0.15m +
            d.AddressScore * 0.20m;

        return Math.Round(weighted, 2);
    }
}