using Maintenance.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maintenance.Domain.Events
{
    public record TicketStatusChangedDomainEvent(Guid TicketId, TicketStatus OldStatus, TicketStatus NewStatus) : INotification;
}
