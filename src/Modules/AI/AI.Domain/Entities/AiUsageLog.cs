using AI.Domain.Enums;
using Domain.Entities;

namespace AI.Domain.Entities
{
    public class AiUsageLog: BaseEntity
    {
        public Guid UserId { get; set; } // 
        public Guid? CompanyId { get; set; }
        public Guid SubscriptionId { get; set; }
        public AiFeatureType FeatureType { get; set; }
        public int TokensUsed { get; set; }
        public string RequestPayload { get; set; } = string.Empty; // Sanitized JSON 
        public string ResponseSummary { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
