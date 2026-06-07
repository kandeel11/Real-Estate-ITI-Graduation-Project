using Document.Application.Interfaces;
using Document.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Document.Infrastructure.Persistence;

public class DocumentDbContext : DbContext  , IDocumentDbContext
{
    public DocumentDbContext(DbContextOptions<DocumentDbContext> options) : base(options) { }

    public DbSet<Domain.Entities.Inquiry> Inquiries => Set<Domain.Entities.Inquiry>();
    public DbSet<InquiryAssignment> InquiryAssignments => Set<InquiryAssignment>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Rating> Ratings => Set<Rating>();
    public DbSet<Domain.Entities.Notification> Notifications => Set<Domain.Entities.Notification>();
    public DbSet<VerificationDocument> VerificationDocuments => Set<VerificationDocument>();
    public DbSet<Lease> Leases => Set<Lease>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("document");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DocumentDbContext).Assembly);
    }
}