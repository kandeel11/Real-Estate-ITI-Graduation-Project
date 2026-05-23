using AI.Domain.Enums;
using Domain.Entities;

namespace AI.Domain.Entities
{
    public class DuplicateDetectionLog:BaseEntity
    {
        public Guid NewPropertyId { get; set; } 
        public Guid SimilarPropertyId { get; set; } 

        public decimal ImageSimilarityScore { get; set; }
        public decimal LocationSimilarityScore { get; set; } 
        public decimal DescriptionSimilarityScore { get; set; } 
        public decimal LayoutSimilarityScore { get; set; } 
        public decimal OverallConfidence { get; set; } 
        public DuplicateDetectionStatus Status { get; set; } = DuplicateDetectionStatus.PendingReview; 
        public Guid? AdminReviewerId { get; set; } 
        public string? AdminNotes { get; set; } 
        public DateTime? ReviewedAt { get; set; } 
    }
}
