using AI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI.Infrastructure.Persistence.Configurations
{
    public class ChatbotMessageConfiguration : IEntityTypeConfiguration<ChatbotMessage>
    {
        public void Configure(EntityTypeBuilder<ChatbotMessage> builder)
        {
            builder.ToTable("ChatbotMessages", "ai");

            builder.HasKey(m => m.Id);

            builder.Property(m => m.Role)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(m => m.Content)
                .HasMaxLength(4000)
                .IsRequired();

            builder.Property(m => m.TokensUsed)
                .IsRequired();
        }
    }
}
