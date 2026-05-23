using AI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AI.Infrastructure.Persistence.Configurations
{
    public class AiChatbotSessionConfiguration : IEntityTypeConfiguration<AiChatbotSession>
    {
        public void Configure(EntityTypeBuilder<AiChatbotSession> builder)
        {
            builder.ToTable("ai_chatbot_sessions", "ai");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

            builder.Property(x => x.SessionToken).HasMaxLength(255).IsRequired();
            builder.HasIndex(x => x.SessionToken).IsUnique();

            builder.Property(x => x.UserId).IsRequired();

            builder.HasMany(x => x.Messages)
                .WithOne(x => x.Session)
                .HasForeignKey(x => x.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasQueryFilter(x => x.DeletedAt == null);
        }
    }
}
