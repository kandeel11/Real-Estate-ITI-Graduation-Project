using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuration بتاع جدول outbox_messages
/// </summary>
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages", "payment");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Type).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Content).HasColumnType("text").IsRequired();
        builder.Property(x => x.OccurredOn).IsRequired();
        builder.Property(x => x.ProcessedOn).IsRequired(false);
        builder.Property(x => x.Error).HasMaxLength(2000).IsRequired(false);

        // Index على الرسائل اللي لسه مش processed — العمود ده هو اللي
        // الـ OutboxPublisherJob بيفلتر عليه كل 10 ثواني
        builder.HasIndex(x => x.ProcessedOn)
            .HasDatabaseName("IX_outbox_messages_unprocessed")
            .HasFilter("\"ProcessedOn\" IS NULL");
    }
}
