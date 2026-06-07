using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Document.Domain.Enums
{ 
    public enum NotificationType
    {
        NewInquiry,
        MaintenanceUpdate,
        SubscriptionExpiration,
        VerificationResult,
        RentPaymentReminder,
        AiReviewResult,
        ContractSigned,
        PaymentReceived,
        DuplicateDetected

    }
}
