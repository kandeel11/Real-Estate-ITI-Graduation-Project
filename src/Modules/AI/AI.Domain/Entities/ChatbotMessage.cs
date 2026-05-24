using Domain.Entities;

namespace AI.Domain.Entities
{
    public class ChatbotMessage : BaseEntity
    {
        public Guid SessionId { get; set; }

        public string Role { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public int TokensUsed { get; set; }

        public virtual ChatbotSession Session { get; set; } = null!;

    }
}