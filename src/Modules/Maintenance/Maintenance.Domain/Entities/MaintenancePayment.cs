using Domain.Entities;
using Maintenance.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maintenance.Domain.Entities
{
    public class MaintenancePayment: BaseEntity
    {
        public Guid TicketId { get; private set; }
        public Guid PayerId { get; private set; }
        public Guid VendorId { get; private set; }
        public decimal Amount { get; private set; }
        public string Currency { get; private set; } = "SAR";
        public MaintenancePaymentStatus Status { get; private set; } = MaintenancePaymentStatus.Pending;
        public string? PaymentMethod { get; private set; }
        public string? TransactionId { get; private set; }
        public DateTime? PaidAt { get; private set; }
        public string? Notes { get; private set; }

        public MaintenanceTicket Ticket { get; private set; } = default!;

        protected MaintenancePayment() { }

        public static MaintenancePayment Create(
            Guid ticketId, Guid payerId, Guid vendorId,
            decimal amount, string currency = "SAR",
            string? paymentMethod = null, string? notes = null)
            => new()
            {
                TicketId = ticketId,
                PayerId = payerId,
                VendorId = vendorId,
                Amount = amount,
                Currency = currency,
                PaymentMethod = paymentMethod,
                Notes = notes
            };

        public void Confirm(string transactionId)
        {
            if (Status != MaintenancePaymentStatus.Pending)
                throw new InvalidOperationException("Payment is not pending.");
            Status = MaintenancePaymentStatus.Paid;
            TransactionId = transactionId;
            PaidAt = DateTime.UtcNow;
            MarkAsUpdated();
        }

        public void Fail()
        {
            Status = MaintenancePaymentStatus.Failed;
            MarkAsUpdated();
        }
    }
}

