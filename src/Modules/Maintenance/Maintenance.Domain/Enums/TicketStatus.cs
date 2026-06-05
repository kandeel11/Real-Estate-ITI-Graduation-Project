using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maintenance.Domain.Enums
{
    public enum TicketStatus {
        Draft,
        Submitted,
        UnderAiReview,
        AiEvaluated,
        WaitingForOwnerDecision,
        WaitingForTenantDecision,
        AssignedToVendor,
        VendorAccepted,
        InProgress,
        AwaitingPayment,
        Completed,
        Cancelled,
        Rejected
    }
}
