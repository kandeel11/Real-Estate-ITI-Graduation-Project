using Domain.Entities;

namespace AI.Domain.Entities
{
    public class AiDescriptionGeneration : BaseEntity
    {
        public Guid PropertyId { get; set; } 
        public Guid RequestedById { get; set; } 

        public string PromptText { get; set; } = string.Empty; 
        public string GeneratedTextEn { get; set; } = string.Empty; 
        public string GeneratedTextAr { get; set; } = string.Empty; 

        public string ModelVersion { get; set; } = string.Empty; 
        public int TokensUsed { get; set; } 
        public bool IsAccepted { get; set; } = false; 
    }
}
