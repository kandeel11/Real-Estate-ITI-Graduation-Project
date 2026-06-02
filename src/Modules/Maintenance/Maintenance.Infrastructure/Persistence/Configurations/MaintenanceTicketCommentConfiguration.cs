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
    public class MaintenanceTicketCommentConfiguration : IEntityTypeConfiguration<MaintenanceTicketComment>
    {
        public void Configure(EntityTypeBuilder<MaintenanceTicketComment> builder)
        {
            builder.ToTable("maintenance_ticket_comments");
            builder.HasKey(x => x.Id);

            // TicketId is a real DB FK (same schema) — configured on the ticket side via HasMany/WithOne
            builder.Property(x => x.TicketId).IsRequired();

            // UserId is a cross-schema ref to identity.users — no DB FK
            builder.Property(x => x.UserId).IsRequired();

            builder.Property(x => x.Content).IsRequired();
            builder.Property(x => x.IsInternal).IsRequired().HasDefaultValue(false);

            // Audit fields from BaseEntity
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.UpdatedAt);
            builder.Property(x => x.DeletedAt);

            builder.HasQueryFilter(x => x.DeletedAt == null);
        }
    }
}
