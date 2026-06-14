using MediatR;
using Property.Domain.Enums;

namespace Property.Application.Features.Commands.SetVerificationStatus;

public record SetVerificationStatusCommand(
    Guid PropertyId,
    VerificationStatus VerificationStatus,
    string? RejectionReason = null
) : IRequest;
