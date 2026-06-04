using MediatR;

namespace AI.Domain.Events
{
    /// <summary>
    /// Raised when an ownership verification is rejected (by admin or AI fake detection).
    /// </summary>
    public record OwnershipRejectedEvent : INotification
    {
        public OwnershipRejectedEvent(Guid verificationRequestId, Guid propertyId, Guid ownerId, string rejectionReason)
        {
            VerificationRequestId = verificationRequestId;
            PropertyId = propertyId;
            OwnerId = ownerId;
            RejectionReason = rejectionReason;
        }

        public Guid VerificationRequestId { get; }

        public Guid PropertyId { get; }

        public Guid OwnerId { get; }

        public string RejectionReason { get; }
    }
}
