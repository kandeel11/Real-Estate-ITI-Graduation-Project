namespace AI.Application.Features.OwnershipVerification.Interfaces
{
    /// <summary>
    /// Repository for OwnershipVerificationRequest persistence.
    /// </summary>
    public interface IOwnershipVerifyRepository
    {
        Task<AI.Domain.Entities.OwnershipVerificationRequest> GetByIdAsync(Guid id);

        Task<AI.Domain.Entities.OwnershipVerificationRequest> GetByPropertyIdAsync(Guid propertyId);

        Task<List<AI.Domain.Entities.OwnershipVerificationRequest>> GetPendingAdminReviewsAsync(int limit = 50);

        Task AddAsync(AI.Domain.Entities.OwnershipVerificationRequest request);

        Task UpdateAsync(AI.Domain.Entities.OwnershipVerificationRequest request);

        Task SaveChangesAsync();
    }
}
