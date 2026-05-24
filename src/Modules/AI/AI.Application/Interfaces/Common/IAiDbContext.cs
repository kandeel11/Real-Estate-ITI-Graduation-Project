using AI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI.Application.Interfaces.Common
{
    public interface IAiDbContext
    {
        // 1. Verification & Duplicate Detection
        DbSet<OwnershipVerificationRequest> OwnershipVerificationRequests { get; }
        DbSet<DuplicateDetectionLog> DuplicateDetectionLogs { get; }

        // 2. Chatbot & Content Generation
        DbSet<AiDescriptionGeneration> AiDescriptionGenerations { get; }
        DbSet<AiChatbotSession> AiChatbotSessions { get; }
        DbSet<AiChatbotMessage> AiChatbotMessages { get; }

        // 3. Search & Pricing
        DbSet<AiSearchQuery> AiSearchQueries { get; }
        DbSet<SavedSearch> SavedSearches { get; }
        DbSet<AiPriceEstimation> AiPriceEstimations { get; }

        // 4. Analytics & Usage Logs
        DbSet<AiUsageLog> AiUsageLogs { get; }
        DbSet<BrokerPerformanceScore> BrokerPerformanceScores { get; }
        DbSet<CompanyAnalytics> CompanyAnalytics { get; }
        DbSet<AdminAnalyticsSnapshot> AdminAnalyticsSnapshots { get; }
        // 5.Chatbot Sessions & Messages
        DbSet<ChatbotMessage> ChatbotMessages { get; }
        DbSet<ChatbotSession> ChatbotSessions { get; }
        // 
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
