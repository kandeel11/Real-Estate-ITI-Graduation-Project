using AI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AI.Infrastructure.Persistence.Configurations
{
    public class AiChatbotMessageConfiguration : IEntityTypeConfiguration<AiChatbotMessage>
    {
        public void Configure(EntityTypeBuilder<AiChatbotMessage> builder)
        {
            builder.ToTable("ai_chatbot_messages", "ai");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

            builder.Property(x => x.Role)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.Content).IsRequired();

        }
    }
}
