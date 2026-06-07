using AI.Application.Interfaces.Common;
using AI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace AI.Infrastructure.Persistence.Contexts;

public class AiDbContext : DbContext,IAiDbContext
{
    public AiDbContext(DbContextOptions<AiDbContext> options)
        : base(options) { }

    // ── Ownership Verification ────────────────────────────────────────────────
    public DbSet<OwnershipVerificationRequest> OwnershipVerificationRequests
        => Set<OwnershipVerificationRequest>();

    // ── Duplicate Detection ───────────────────────────────────────────────────
    public DbSet<DuplicateDetectionLog> DuplicateDetectionLogs
        => Set<DuplicateDetectionLog>();

    // ── Lease Extraction ──────────────────────────────────────────────────────
    // ✅ Fixed — was missing => Set<>()
    public DbSet<LeaseExtractionRequest> LeaseExtractionRequests
        => Set<LeaseExtractionRequest>();

    // ── AI Description Generator ──────────────────────────────────────────────
    public DbSet<AiDescriptionGeneration> AiDescriptionGenerations
        => Set<AiDescriptionGeneration>();

    // ── AI Chatbot ────────────────────────────────────────────────────────────
    // ✅ Removed duplicate ChatbotSessions / ChatbotMessages
    //    Keep only the Ai-prefixed versions (consistent naming)
    public DbSet<AiChatbotSession> AiChatbotSessions
        => Set<AiChatbotSession>();

    public DbSet<AiChatbotMessage> AiChatbotMessages
        => Set<AiChatbotMessage>();

    // ── Smart Search ──────────────────────────────────────────────────────────
    public DbSet<AiSearchQuery> AiSearchQueries
        => Set<AiSearchQuery>();

    public DbSet<SavedSearch> SavedSearches
        => Set<SavedSearch>();

    // ── Price Estimation ──────────────────────────────────────────────────────
    public DbSet<AiPriceEstimation> AiPriceEstimations
        => Set<AiPriceEstimation>();

    // ── Broker Performance ────────────────────────────────────────────────────
    public DbSet<BrokerPerformanceScore> BrokerPerformanceScores
        => Set<BrokerPerformanceScore>();

    // ── Company Analytics ─────────────────────────────────────────────────────
    public DbSet<CompanyAnalytics> CompanyAnalytics
        => Set<CompanyAnalytics>();

    public DbSet<AdminAnalyticsSnapshot> AdminAnalyticsSnapshots
        => Set<AdminAnalyticsSnapshot>();

    // ── AI Usage Logs (shared across all features) ────────────────────────────
    public DbSet<AiUsageLog> AiUsageLogs
        => Set<AiUsageLog>();

    // ─────────────────────────────────────────────────────────────────────────

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ✅ All AI tables go under "ai" schema in SQL Server
        modelBuilder.HasDefaultSchema("ai");

        // Auto-discovers all IEntityTypeConfiguration<T> in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(
            Assembly.GetExecutingAssembly());

        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
        => base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
}