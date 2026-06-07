namespace Identity.Application.DTOs;

public record CompanyDto(
    Guid Id,
    string Name,
    string CommercialRegister,
    string City,
    string Address,
    string Phone,
    string Email,
    bool IsVerified,
    decimal RatingAvg,
    int TotalRatings);
