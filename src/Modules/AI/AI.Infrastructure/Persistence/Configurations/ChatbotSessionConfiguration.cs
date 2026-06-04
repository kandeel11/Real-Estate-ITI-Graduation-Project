using AI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AI.Infrastructure.Persistence.Configurations
{
    public class ChatbotSessionConfiguration : IEntityTypeConfiguration<ChatbotSession>
    {
        public void Configure(EntityTypeBuilder<ChatbotSession> builder)
        {
            builder.ToTable("ChatbotSessions", "ai");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.SessionToken)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(s => s.Title)
                .HasMaxLength(200)
                .IsRequired();

            builder.HasMany(s => s.Messages)
                .WithOne(m => m.Session)
                .HasForeignKey(m => m.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
