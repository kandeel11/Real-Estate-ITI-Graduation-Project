using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maintenance.Domain.Events
{
    public record CommentAddedDomainEvent(Guid CommentId, Guid TicketId, Guid AuthorUserId) : INotification;
}
