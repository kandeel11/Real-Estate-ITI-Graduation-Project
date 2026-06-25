using MediatR;

namespace Document.Application.Commands.Lease.SetLeaseManagementOptions;

public record SetLeaseManagementOptionsCommand(
    Guid LeaseId,
    List<string> ManagementOptions,  // e.g. ["Maintenance", "PaymentCollection"]
    Guid? ManagerUserId              // company manager's UserId — receives notification when partial management
) : IRequest<SetLeaseManagementOptionsResult>;

public record SetLeaseManagementOptionsResult(
    bool IsFullManagement,
    bool RequiresManagerApproval,
    string ApprovalStatus   // "NotRequired" | "PendingApproval"
);
