using MediatR;

namespace AI.Domain.Events
{
    /// <summary>
    /// Raised when ownership verification score is below threshold and requires admin review.
    /// </summary>
    public record OwnershipSentToAdminReviewEvent : INotification
    {
        public OwnershipSentToAdminReviewEvent(Guid verificationRequestId, Guid propertyId, Guid ownerId, decimal confidenceScore, string reason)
        {
            VerificationRequestId = verificationRequestId;
            PropertyId = propertyId;
            OwnerId = ownerId;
            ConfidenceScore = confidenceScore;
            Reason = reason;
        }

        public Guid VerificationRequestId { get; }

        public Guid PropertyId { get; }

        public Guid OwnerId { get; }

        public decimal ConfidenceScore { get; }

        public string Reason { get; }
    }
}
