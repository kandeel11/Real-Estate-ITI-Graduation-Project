using Document.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Document.Infrastructure.Persistence.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> b)
    {
        b.ToTable("conversations");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.PropertyId).IsRequired();
        b.Property(x => x.TenantId).IsRequired();
        b.Property(x => x.BrokerOrOwnerId).IsRequired();
        b.Property(x => x.IsActive).IsRequired();
        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.UpdatedAt).IsRequired();
        b.HasMany(x => x.Messages).WithOne(x => x.Conversation)
            .HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
    }
}