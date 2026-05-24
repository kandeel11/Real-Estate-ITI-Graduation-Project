using Domain.Entities;

namespace AI.Domain.Entities
{
    public class AiChatbotSession : BaseEntity
    {
        public Guid UserId { get; set; }
        public string SessionToken { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime? EndedAt { get; set; }

        // Navigation property (Same schema relation)
        public virtual ICollection<AiChatbotMessage> Messages { get; set; } = new List<AiChatbotMessage>();
    }
}

