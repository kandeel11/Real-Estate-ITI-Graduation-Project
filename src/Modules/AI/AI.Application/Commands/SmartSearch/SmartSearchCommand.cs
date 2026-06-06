// AI.Application/Commands/SmartSearch/SmartSearchCommand.cs

using AI.Application.DTOs;
using MediatR;

namespace AI.Application.Commands.SmartSearch;

public sealed record SmartSearchCommand(
    Guid? UserId,
    string RawQuery,
    int Page = 1,
    int PageSize = 20
) : IRequest<SmartSearchResult>;