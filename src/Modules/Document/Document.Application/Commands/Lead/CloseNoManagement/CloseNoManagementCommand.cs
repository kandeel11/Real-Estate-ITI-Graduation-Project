using MediatR;

namespace Document.Application.Commands.Lead.CloseNoManagement;

/// <summary>
/// Closes a deal without a lease or management service.
/// CompanyManagerId == null  →  independent broker: property rented immediately.
/// CompanyManagerId != null  →  company broker "no management": notifies manager to confirm.
/// </summary>
public record CloseNoManagementCommand(
    Guid LeadId,
    Guid? CompanyManagerId
) : IRequest<CloseNoManagementResult>;

public record CloseNoManagementResult(
    string Status   // "closed" | "pending_manager_confirmation"
);
