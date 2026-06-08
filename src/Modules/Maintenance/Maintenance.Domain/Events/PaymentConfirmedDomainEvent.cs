using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maintenance.Domain.Events
{
    public record PaymentConfirmedDomainEvent(Guid TicketId, Guid PaymentId, decimal Amount) : INotification;
}
