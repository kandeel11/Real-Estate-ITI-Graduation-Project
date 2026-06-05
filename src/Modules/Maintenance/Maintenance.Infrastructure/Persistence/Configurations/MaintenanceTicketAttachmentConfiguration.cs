using Maintenance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maintenance.Infrastructure.Persistence.Configurations;

public class MaintenanceTicketAttachmentConfiguration : IEntityTypeConfiguration<MaintenanceTicketAttachment>
{
    public void Configure(EntityTypeBuilder<MaintenanceTicketAttachment> b)
    {
        b.ToTable("maintenance_ticket_attachments");
        b.HasKey(x => x.Id);
        b.Property(x => x.TicketId).IsRequired();
        b.Property(x => x.UploadedById).IsRequired();
        b.Property(x => x.FileUrl).IsRequired().HasMaxLength(500);
        b.Property(x => x.FileType).IsRequired().HasMaxLength(100);
        b.Property(x => x.FileSizeKb).IsRequired();
        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.UpdatedAt);
        b.Property(x => x.DeletedAt);
        b.HasQueryFilter(x => x.DeletedAt == null);
    }
}