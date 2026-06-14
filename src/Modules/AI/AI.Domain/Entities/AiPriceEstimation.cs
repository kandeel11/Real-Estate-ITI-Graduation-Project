using Domain.Entities;

namespace AI.Domain.Entities
{
    public class AiPriceEstimation : BaseEntity
    {
        public Guid PropertyId { get; set; } // 
        public Guid RequestedById { get; set; }
        public decimal EstimatedPriceMin { get; set; }
        public decimal EstimatedPriceMax { get; set; }
        public decimal EstimatedPrice { get; set; } // Point estimate 
        public decimal ConfidenceScore { get; set; }
        public string ComparableProperties { get; set; } = string.Empty; // JSON array
        public string ModelVersion { get; set; } = string.Empty;
    }
}
