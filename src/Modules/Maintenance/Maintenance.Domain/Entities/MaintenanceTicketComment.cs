using Domain.Entities;
using Maintenance.Domain.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maintenance.Domain.Entities
{
    public class MaintenanceTicketComment : BaseEntity
    {
        public Guid TicketId { get; private set; }
        public Guid UserId { get; private set; }
        public string Content { get; private set; } = default!;
        public bool IsInternal { get; private set; }
        public MaintenanceTicket Ticket { get; private set; } = default!;
        protected MaintenanceTicketComment() { }

        public static MaintenanceTicketComment Create(
            Guid ticketId, Guid userId, string content, bool isInternal)
        {
            var comment = new MaintenanceTicketComment
            {
                TicketId = ticketId,
                UserId = userId,
                Content = content,
                IsInternal = isInternal
            };
            if (!isInternal)
                comment.AddDomainEvent(new CommentAddedDomainEvent(comment.Id, ticketId, userId));

            return comment;
        }
        public void EditContent(string newContent)
        {
            Content = newContent;
            MarkAsUpdated();
        }
        public void Remove() => MarkAsDeleted();
    }
}
