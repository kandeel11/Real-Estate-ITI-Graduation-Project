using AI.Application.Features.OwnershipVerification.Interfaces;
using AI.Domain.Entities;
using AI.Domain.Enums;
using AI.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace AI.Infrastructure.Persistence.Repositories
{
    public class OwnershipVerifyRepository : IOwnershipVerifyRepository
    {
        private readonly AiDbContext _context;

        public OwnershipVerifyRepository(AiDbContext context)
        {
            _context = context;
        }

        public async Task<OwnershipVerificationRequest> GetByIdAsync(Guid id)
        {
            return await _context.OwnershipVerificationRequests
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<OwnershipVerificationRequest> GetByPropertyIdAsync(Guid propertyId)
        {
            return await _context.OwnershipVerificationRequests
                .Where(x => x.PropertyId == propertyId && 
                           (x.Status == VerificationStatus.Pending || 
                            x.Status == VerificationStatus.RequiresManualReview))
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<List<OwnershipVerificationRequest>> GetPendingAdminReviewsAsync(int limit = 50)
        {
            return await _context.OwnershipVerificationRequests
                .Where(x => x.Status == VerificationStatus.RequiresManualReview)
                .OrderBy(x => x.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task AddAsync(OwnershipVerificationRequest request)
        {
            await _context.OwnershipVerificationRequests.AddAsync(request);
        }

        public async Task UpdateAsync(OwnershipVerificationRequest request)
        {
            _context.OwnershipVerificationRequests.Update(request);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
