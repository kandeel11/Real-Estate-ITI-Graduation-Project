using Maintenance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maintenance.Infrastructure.Persistence.Configurations;

public class TicketAiEvaluationConfiguration : IEntityTypeConfiguration<TicketAiEvaluation>
{
    public void Configure(EntityTypeBuilder<TicketAiEvaluation> b)
    {
        b.ToTable("maintenance_ticket_ai_evaluations");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.TicketId).IsRequired();
        b.HasIndex(x => x.TicketId).IsUnique();
        b.Property(x => x.ResponsibleParty).IsRequired().HasMaxLength(50);
        b.Property(x => x.Category).IsRequired().HasMaxLength(255);
        b.Property(x => x.ConfidenceScore).IsRequired();
        b.Property(x => x.Reasoning).IsRequired();
        b.Property(x => x.EvaluatedAt).IsRequired();
        b.Property(x => x.CreatedAt).IsRequired();
    }
}