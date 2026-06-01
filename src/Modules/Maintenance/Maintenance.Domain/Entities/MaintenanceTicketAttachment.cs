using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maintenance.Domain.Entities
{

    public class MaintenanceTicketAttachment : BaseEntity
    {
        public Guid TicketId { get; private set; }
        public Guid UploadedById { get; private set; }
        public string FileUrl { get; private set; } = default!;
        public string FileType { get; private set; } = default!;
        public int FileSizeKb { get; private set; }
        public MaintenanceTicket Ticket { get; private set; } = default!;
        protected MaintenanceTicketAttachment() { }
        public static MaintenanceTicketAttachment Create(
            Guid ticketId, string fileUrl, string fileType, int fileSizeKb, Guid uploadedById)
            => new()
            {
                TicketId = ticketId,
                FileUrl = fileUrl,
                FileType = fileType,
                FileSizeKb = fileSizeKb,
                UploadedById = uploadedById
            };
        public void Remove() => MarkAsDeleted();
    }
}
