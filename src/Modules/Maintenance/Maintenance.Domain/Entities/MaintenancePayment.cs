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

        // ── Stripe Checkout ───────────────────────────────────────────────────
        public string? CheckoutSessionId { get; private set; }
        public string? CheckoutUrl { get; private set; }

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

        public void MarkUserPaid(string transactionId)
        {
            if (Status != MaintenancePaymentStatus.Pending)
                throw new InvalidOperationException("Payment is not pending.");
            if (TransactionId is not null)
                throw new InvalidOperationException("Payment has already been submitted.");
            TransactionId = transactionId;
            PaymentMethod = "Online";
            MarkAsUpdated();
        }

        public void Confirm()
        {
            if (Status != MaintenancePaymentStatus.Pending)
                throw new InvalidOperationException("Payment is not pending.");
            if (TransactionId is null)
                throw new InvalidOperationException("User has not submitted payment yet.");
            Status = MaintenancePaymentStatus.Paid;
            PaidAt = DateTime.UtcNow;
            MarkAsUpdated();
        }

        public void SetCheckoutSession(string sessionId, string checkoutUrl)
        {
            CheckoutSessionId = sessionId;
            CheckoutUrl = checkoutUrl;
            MarkAsUpdated();
        }

        /// <summary>
        /// Idempotent confirmation from Stripe webhook. Returns false when already Paid.
        /// </summary>
        public bool TryConfirmViaWebhook(string stripeTransactionId)
        {
            if (Status == MaintenancePaymentStatus.Paid)
                return false;
            TransactionId = stripeTransactionId;
            PaymentMethod = "Stripe";
            Status = MaintenancePaymentStatus.Paid;
            PaidAt = DateTime.UtcNow;
            MarkAsUpdated();
            return true;
        }

        public void Fail()
        {
            Status = MaintenancePaymentStatus.Failed;
            MarkAsUpdated();
        }
    }
}

