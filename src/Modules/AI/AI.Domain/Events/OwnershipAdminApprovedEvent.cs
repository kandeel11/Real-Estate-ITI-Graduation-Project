using MediatR;

namespace AI.Domain.Events
{
    /// <summary>
    /// Raised when admin approves an ownership verification request.
    /// </summary>
    public record OwnershipAdminApprovedEvent : INotification
    {
        public OwnershipAdminApprovedEvent(Guid verificationRequestId, Guid propertyId, Guid ownerId, Guid adminId, string reviewNotes)
        {
            VerificationRequestId = verificationRequestId;
            PropertyId = propertyId;
            OwnerId = ownerId;
            AdminId = adminId;
            ReviewNotes = reviewNotes;
        }

        public Guid VerificationRequestId { get; }

        public Guid PropertyId { get; }

        public Guid OwnerId { get; }

        public Guid AdminId { get; }

        public string ReviewNotes { get; }
    }
}
