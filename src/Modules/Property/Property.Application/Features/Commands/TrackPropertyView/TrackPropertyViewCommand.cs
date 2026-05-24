using MediatR;
using Property.Domain.Enums;

namespace Property.Application.Features.Commands.TrackPropertyView;

public record TrackPropertyViewCommand(
    Guid PropertyId,
    Guid? ViewerId,
    string? IpAddress,
    ViewSource? Source
) : IRequest;
