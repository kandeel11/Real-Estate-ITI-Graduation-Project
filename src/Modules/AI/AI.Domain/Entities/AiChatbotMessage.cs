using AI.Domain.Enums;
using Domain.Entities;

namespace AI.Domain.Entities
{
    public class AiChatbotMessage: BaseEntity
    {
        public Guid SessionId { get; set; } // FK to AiChatbotSession 
        public ChatbotRole Role { get; set; } // User or Assistant 
        public string Content { get; set; } = string.Empty; // 
        public int TokensUsed { get; set; } // 
        // Navigation property
        public virtual AiChatbotSession Session { get; set; } = null!;
    }
}