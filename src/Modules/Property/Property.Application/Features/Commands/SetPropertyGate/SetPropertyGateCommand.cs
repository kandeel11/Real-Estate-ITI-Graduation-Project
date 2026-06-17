using MediatR;
using Property.Domain.Enums;

namespace Property.Application.Features.Commands.SetPropertyGate;

public enum PropertyGateType
{
    Ownership,
    Duplicate
}

/// <summary>
/// Sets one of a property's two verification gates (ownership or duplicate). The entity
/// re-evaluates both gates and derives its lifecycle status (Active / Rejected / RequiresManualReview).
/// </summary>
public record SetPropertyGateCommand(
    Guid PropertyId,
    PropertyGateType Gate,
    GateStatus Status,
    string? Note = null
) : IRequest;
