using Domain.Entities;

namespace AI.Domain.Entities
{
    public class AdminAnalyticsSnapshot: BaseEntity
    {
        public DateOnly SnapshotDate { get; set; } //
        public int TotalUsers { get; set; } //
        public int TotalCompanies { get; set; } //
        public int TotalProperties { get; set; } //
        public int TotalActiveLeases { get; set; } //
        public decimal TotalRevenue { get; set; } //
        public int NewSubscriptions { get; set; } //
        public int ChurnedSubscriptions { get; set; } //
        public int TotalAiRequests { get; set; } //
        public string MostActiveCities { get; set; } = string.Empty; // JSON
        public string MostPopularPlans { get; set; } = string.Empty; // JSON
        public string MostActiveCompanies { get; set; } = string.Empty; // JSON
        public string VerificationStats { get; set; } = string.Empty; // JSON
    }
}
