using AI.Domain.Enums;
using Domain.Entities;

namespace AI.Domain.Entities
{
    public sealed class AiUsageLog : BaseEntity
    {
        public Guid UserId { get; set; } // 
        public Guid? CompanyId { get; set; }
        public Guid? SubscriptionId { get; set; }
        public AiFeatureType FeatureType { get; set; }
        public int TokensUsed { get; set; }
        public string RequestPayload { get; set; } = string.Empty; // Sanitized JSON 
        public string ResponseSummary { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        private AiUsageLog() { }

        public static AiUsageLog Create(
            Guid userId,
            Guid? companyId,
            Guid? subscriptionId,
            AiFeatureType featureType,
            int tokensUsed,
            string requestPayload,
            string responseSummary,
            bool success,
            string? errorMessage) => new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CompanyId = companyId,
                SubscriptionId = subscriptionId,
                FeatureType = featureType,
                TokensUsed = tokensUsed,
                RequestPayload = requestPayload,
                ResponseSummary = responseSummary,
                Success = success,
                ErrorMessage = errorMessage,
                CreatedAt = DateTime.UtcNow
            };
    
    }
}
