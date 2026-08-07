// AI.Infrastructure/Persistence/Configurations/AiSearchQueryConfiguration.cs

using AI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AI.Infrastructure.Persistence.Configurations;

public sealed class AiSearchQueryConfiguration
    : IEntityTypeConfiguration<AiSearchQuery>
{
    public void Configure(EntityTypeBuilder<AiSearchQuery> b)
    {
        b.ToTable("ai_search_queries", "ai");
        b.HasKey(x => x.Id);

        b.Property(x => x.RawQuery).IsRequired().HasMaxLength(1000);
        b.Property(x => x.Language).IsRequired().HasMaxLength(10);
        b.Property(x => x.ParsedFilters).IsRequired().HasColumnType("text");
        b.Property(x => x.ParsingConfidence).HasPrecision(5, 4);

        b.HasIndex(x => x.UserId).HasDatabaseName("ix_ai_search_queries_user");
        b.HasIndex(x => x.CreatedAt).HasDatabaseName("ix_ai_search_queries_date");
    }
}