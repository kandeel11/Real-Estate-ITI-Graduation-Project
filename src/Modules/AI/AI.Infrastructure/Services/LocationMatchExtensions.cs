// LocationMatchDetails helper
public static class LocationMatchExtensions
{
    public static decimal AverageScore(this LocationMatchDetails d)
    {
        // Weighted average — country/governorate/city carry more weight than address
        var weighted =
            d.CountryScore * 0.10m +
            d.GovernorateScore * 0.25m +
            d.CityScore * 0.30m +
            d.DistrictScore * 0.15m +
            d.AddressScore * 0.20m;
        return Math.Round(weighted, 2);
    }

}
