using Maintenance.Domain.Entities;
using Maintenance.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maintenance.Infrastructure.Persistence.Configurations
{

    public class MaintenanceTicketConfiguration : IEntityTypeConfiguration<MaintenanceTicket>
    {
        public void Configure(EntityTypeBuilder<MaintenanceTicket> builder)
        {
            builder.ToTable("maintenance_tickets");
            builder.HasKey(x => x.Id);
            builder.HasQueryFilter(x => x.DeletedAt == null);
            builder.Property(x => x.PropertyId).IsRequired();
            builder.Property(x => x.TenantId).IsRequired();
            builder.Property(x => x.LeaseId);
            builder.Property(x => x.MaintenanceCompanyId);

            builder.Property(x => x.Title).IsRequired().HasMaxLength(255);
            builder.Property(x => x.Description).IsRequired();
            builder.Property(x => x.AiPredictedSla).HasMaxLength(100);
            builder.Property(x => x.Priority)
                .IsRequired()
                .HasConversion<string>()
                .HasDefaultValue(TicketPriority.Medium);

            builder.Property(x => x.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasDefaultValue(TicketStatus.Open);

            builder.Property(x => x.ResolvedAt);
            builder.Property(x => x.ClosedAt);

            // fields from BaseEntity
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.UpdatedAt);
            builder.Property(x => x.DeletedAt);

            // Indexes
            builder.HasIndex(x => x.TenantId);
            builder.HasIndex(x => x.PropertyId);
            builder.HasIndex(x => x.Status);

            // One ticket → many attachments (same schema, real FK)
            builder.HasMany(x => x.Attachments)
                .WithOne(a => a.Ticket)
                .HasForeignKey(a => a.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            // One ticket → many comments (same schema, real FK)
            builder.HasMany(x => x.Comments)
                .WithOne(c => c.Ticket)
                .HasForeignKey(c => c.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
