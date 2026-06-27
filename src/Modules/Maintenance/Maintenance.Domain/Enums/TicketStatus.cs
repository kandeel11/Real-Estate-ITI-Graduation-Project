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
        /// <summary>
        /// A Tenant, Owner, or Company Manager has disputed the AI's responsibility decision.
        /// An Admin must resolve the dispute via ResolveTicketDisputeCommand before the
        /// ticket can continue its normal lifecycle.
        /// </summary>
        Disputed,
        AssignedToVendor,
        VendorAccepted,
        InProgress,
        AwaitingPayment,
        Completed,
        Cancelled,
        Rejected
    }
}
