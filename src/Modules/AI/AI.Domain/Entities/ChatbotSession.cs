using Domain.Entities;

namespace AI.Domain.Entities
{
    public class ChatbotSession : BaseEntity
    {
        public Guid UserId { get; set; }

        public string SessionToken { get; set; } = string.Empty;

        public string Title { get; set; } = "New Chat";

        public bool IsActive { get; set; } = true;

        public virtual ICollection<ChatbotMessage> Messages { get; set; } = new HashSet<ChatbotMessage>();
    }
}
