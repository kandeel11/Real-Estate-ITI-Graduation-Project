namespace Identity.Infrastructure.Services;

public class JwtOptions
{
    public string SigningKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "RealEstate";
    public string Audience { get; set; } = "RealEstateApp";
}
