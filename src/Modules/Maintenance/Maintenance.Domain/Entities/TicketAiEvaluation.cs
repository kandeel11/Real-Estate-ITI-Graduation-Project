using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maintenance.Domain.Entities
{
    public class TicketAiEvaluation : BaseEntity
    {
        public Guid TicketId { get; private set; }
        public string ResponsibleParty { get; private set; } = default!;
        public string Category { get; private set; } = default!;
        public int ConfidenceScore { get; private set; }
        public string Reasoning { get; private set; } = default!;
        public DateTime EvaluatedAt { get; private set; }
        public string? RawRequestJson { get; private set; }
        public string? RawResponseJson { get; private set; }

        public MaintenanceTicket Ticket { get; private set; } = default!;

        protected TicketAiEvaluation() { }

        public static TicketAiEvaluation Create(
            Guid ticketId, string responsibleParty, string category,
            int confidenceScore, string reasoning,
            string? rawRequest = null, string? rawResponse = null)
            => new()
            {
                TicketId = ticketId,
                ResponsibleParty = responsibleParty,
                Category = category,
                ConfidenceScore = confidenceScore,
                Reasoning = reasoning,
                EvaluatedAt = DateTime.UtcNow,
                RawRequestJson = rawRequest,
                RawResponseJson = rawResponse
            };
    }
}
