// AI.Application/DTOs/SmartSearchRequest.cs

namespace AI.Application.DTOs;

public sealed record SmartSearchRequest(
    Guid? UserId,
    string RawQuery,     // "2-bed apartment near university under 8000"
    int Page = 1,
    int PageSize = 20
);