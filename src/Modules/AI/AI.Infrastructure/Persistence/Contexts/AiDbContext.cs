using AI.Application.Interfaces.Common;
using AI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace AI.Infrastructure.Persistence.Contexts
{
    public class AiDbContext : DbContext, IAiDbContext
    {
        public AiDbContext(DbContextOptions<AiDbContext> options) : base(options)
        {
        }
        public DbSet<OwnershipVerificationRequest> OwnershipVerificationRequests => Set<OwnershipVerificationRequest>();

        public DbSet<DuplicateDetectionLog> DuplicateDetectionLogs => Set<DuplicateDetectionLog>();

        public DbSet<AiDescriptionGeneration> AiDescriptionGenerations => Set<AiDescriptionGeneration>();

        public DbSet<AiChatbotSession> AiChatbotSessions => Set<AiChatbotSession>();

        public DbSet<AiChatbotMessage> AiChatbotMessages => Set<AiChatbotMessage>();

        public DbSet<AiSearchQuery> AiSearchQueries => Set<AiSearchQuery>();

        public DbSet<SavedSearch> SavedSearches => Set<SavedSearch>();

        public DbSet<AiPriceEstimation> AiPriceEstimations => Set<AiPriceEstimation>();

        public DbSet<AiUsageLog> AiUsageLogs => Set<AiUsageLog>();

        public DbSet<BrokerPerformanceScore> BrokerPerformanceScores => Set<BrokerPerformanceScore>();

        public DbSet<CompanyAnalytics> CompanyAnalytics => Set<CompanyAnalytics>();

        public DbSet<AdminAnalyticsSnapshot> AdminAnalyticsSnapshots => Set<AdminAnalyticsSnapshot>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("ai");

            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            base.OnModelCreating(modelBuilder);
        }
        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
    }
}
