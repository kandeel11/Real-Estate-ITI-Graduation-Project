using MediatR;

namespace AI.Domain.Events
{
    /// <summary>
    /// Raised when ownership is automatically verified by AI (score >= threshold).
    /// </summary>
    public record OwnershipAutoVerifiedEvent : INotification
    {
        public OwnershipAutoVerifiedEvent(Guid verificationRequestId, Guid propertyId, Guid ownerId, decimal confidenceScore)
        {
            VerificationRequestId = verificationRequestId;
            PropertyId = propertyId;
            OwnerId = ownerId;
            ConfidenceScore = confidenceScore;
        }

        public Guid VerificationRequestId { get; }

        public Guid PropertyId { get; }

        public Guid OwnerId { get; }

        public decimal ConfidenceScore { get; }
    }
}
