// AI.Application/Interfaces/Common/IAiDbContext.cs

using AI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace AI.Application.Interfaces.Common;

public interface IAiDbContext
{
    // ── Ownership Verification ────────────────────────────────────────────────
    DbSet<OwnershipVerificationRequest> OwnershipVerificationRequests { get; }

    // ── Duplicate Detection ───────────────────────────────────────────────────
    DbSet<DuplicateDetectionLog> DuplicateDetectionLogs { get; }

    // ── Lease Extraction ──────────────────────────────────────────────────────
    DbSet<LeaseExtractionRequest> LeaseExtractionRequests { get; }

    // ── AI Description Generator ──────────────────────────────────────────────
    DbSet<AiDescriptionGeneration> AiDescriptionGenerations { get; }

    // ── AI Chatbot ────────────────────────────────────────────────────────────
    DbSet<AiChatbotSession> AiChatbotSessions { get; }
    DbSet<AiChatbotMessage> AiChatbotMessages { get; }

    DbSet<ChatbotSession> ChatbotSessions { get; }
    DbSet<ChatbotMessage> ChatbotMessages { get; }

    // ── Smart Search ──────────────────────────────────────────────────────────
    DbSet<AiSearchQuery> AiSearchQueries { get; }
    DbSet<SavedSearch> SavedSearches { get; }

    // ── Price Estimation ──────────────────────────────────────────────────────
    DbSet<AiPriceEstimation> AiPriceEstimations { get; }

    // ── Broker Performance ────────────────────────────────────────────────────
    DbSet<BrokerPerformanceScore> BrokerPerformanceScores { get; }

    // ── Company Analytics ─────────────────────────────────────────────────────
    DbSet<CompanyAnalytics> CompanyAnalytics { get; }
    DbSet<AdminAnalyticsSnapshot> AdminAnalyticsSnapshots { get; }

    // ── AI Usage Logs ─────────────────────────────────────────────────────────
    DbSet<AiUsageLog> AiUsageLogs { get; }

    // ── EF Core operations ───────────────────────────────────────────────────
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gives handlers access to Database for transactions and raw SQL.
    /// e.g. await _context.Database.BeginTransactionAsync()
    /// </summary>
    DatabaseFacade Database { get; }
}