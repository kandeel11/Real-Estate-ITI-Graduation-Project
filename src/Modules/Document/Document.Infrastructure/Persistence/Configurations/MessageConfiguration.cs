using Document.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Document.Infrastructure.Persistence.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> b)
    {
        b.ToTable("messages");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.SenderId).IsRequired();
        b.Property(x => x.Content).HasColumnType("text").IsRequired();
        b.Property(x => x.IsRead).HasDefaultValue(false);
        b.Property(x => x.CreatedAt).IsRequired();
        b.HasQueryFilter(x => x.DeletedAt == null);
        b.HasIndex(x => x.ConversationId);
    }
}