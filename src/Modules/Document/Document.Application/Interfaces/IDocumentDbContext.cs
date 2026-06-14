using Document.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Document.Application.Interfaces;

public interface IDocumentDbContext
{
    DbSet<Inquiry> Inquiries { get; }
    DbSet<InquiryAssignment> InquiryAssignments { get; }
    DbSet<Lead> Leads { get; }
    DbSet<Lease> Leases { get; }
    DbSet<Conversation> Conversations { get; }
    DbSet<Message> Messages { get; }
    DbSet<Rating> Ratings { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<VerificationDocument> VerificationDocuments { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}