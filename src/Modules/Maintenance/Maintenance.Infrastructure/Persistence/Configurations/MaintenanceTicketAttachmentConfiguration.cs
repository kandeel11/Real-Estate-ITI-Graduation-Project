using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Maintenance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maintenance.Infrastructure.Persistence.Configurations
{
    public class MaintenanceTicketAttachmentConfiguration : IEntityTypeConfiguration<MaintenanceTicketAttachment>
    {
        public void Configure(EntityTypeBuilder<MaintenanceTicketAttachment> builder)
        {
            builder.ToTable("maintenance_ticket_attachments");
            builder.HasKey(x => x.Id);

            // TicketId is a real DB FK (same schema) — configured on the ticket side via HasMany/WithOne
            builder.Property(x => x.TicketId).IsRequired();

            // UploadedById is a cross-schema ref to identity.users — no DB FK
            builder.Property(x => x.UploadedById).IsRequired();

            builder.Property(x => x.FileUrl).IsRequired().HasMaxLength(500);
            builder.Property(x => x.FileType).IsRequired().HasMaxLength(100);
            builder.Property(x => x.FileSizeKb).IsRequired();

            // Audit fields from BaseEntity
            // UpdatedAt intentionally stays null — attachments are immutable after upload
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.UpdatedAt);
            builder.Property(x => x.DeletedAt);

            builder.HasQueryFilter(x => x.DeletedAt == null);
        }
    }
}
