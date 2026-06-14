using Domain.Entities;
using System;
using System.Collections.Generic;

namespace AI.Domain.Entities
{
    public class ChatbotSession : BaseEntity
    {
        public Guid UserId { get; private set; }

        public string SessionToken { get; private set; } = string.Empty;

        public string Title { get; private set; } = "New Chat";

        public bool IsActive { get; private set; } = true;

        public virtual ICollection<ChatbotMessage> Messages { get; private set; } = new HashSet<ChatbotMessage>();

        // Parameterless constructor required by EF Core
        protected ChatbotSession() { }

        // Factory Method
        public static ChatbotSession Create(Guid userId, string sessionToken)
        {
            return new ChatbotSession
            {
                UserId = userId,
                SessionToken = sessionToken,
                Title = "New Chat",
                IsActive = true
            };
        }

        public void EndSession()
        {
            IsActive = false;
        }
    }
}