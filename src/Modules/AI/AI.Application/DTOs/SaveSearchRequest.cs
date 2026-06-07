// AI.Application/DTOs/SaveSearchRequest.cs

namespace AI.Application.DTOs;

public sealed record SaveSearchRequest(
    Guid UserId,
    Guid QueryId,       // from SmartSearchResult.QueryId
    string Name,          // user gives it a name: "Maadi 2bed search"
    bool NotifyOnMatch  // true = notify when new property matches
);