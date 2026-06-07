// AI.Infrastructure/Persistence/Configurations/AiChatbotMessageConfiguration.cs

using AI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AI.Infrastructure.Persistence.Configurations;

public sealed class AiChatbotMessageConfiguration
    : IEntityTypeConfiguration<AiChatbotMessage>
{
    public void Configure(EntityTypeBuilder<AiChatbotMessage> b)
    {
        b.ToTable("AiChatbotMessages");

        b.HasKey(x => x.Id);

        b.Property(x => x.Role)
         .IsRequired()
         .HasMaxLength(20);           // "user" | "assistant"

        b.Property(x => x.Content)
         .IsRequired()
         .HasColumnType("nvarchar(max)");

        b.HasIndex(x => x.SessionId)
         .HasDatabaseName("IX_AiChatbotMessages_SessionId");
    }
}