// AI.Infrastructure/Persistence/Configurations/AiChatbotSessionConfiguration.cs

using AI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AI.Infrastructure.Persistence.Configurations;

public sealed class AiChatbotSessionConfiguration
    : IEntityTypeConfiguration<AiChatbotSession>
{
    public void Configure(EntityTypeBuilder<AiChatbotSession> b)
    {
        b.ToTable("AiChatbotSessions");

        b.HasKey(x => x.Id);

        b.Property(x => x.SessionToken)
         .IsRequired()
         .HasMaxLength(200);

        b.HasMany(x => x.Messages)
         .WithOne(x => x.Session)
         .HasForeignKey(x => x.SessionId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => x.UserId)
         .HasDatabaseName("IX_AiChatbotSessions_UserId");

        b.HasIndex(x => x.SessionToken)
         .IsUnique()
         .HasDatabaseName("IX_AiChatbotSessions_SessionToken");
    }
}