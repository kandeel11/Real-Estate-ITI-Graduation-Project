// AI.Infrastructure/Persistence/Configurations/SavedSearchConfiguration.cs

using AI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AI.Infrastructure.Persistence.Configurations;

public sealed class SavedSearchConfiguration
    : IEntityTypeConfiguration<SavedSearch>
{
    public void Configure(EntityTypeBuilder<SavedSearch> b)
    {
        b.ToTable("saved_searches", "ai");
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.RawQuery).IsRequired().HasMaxLength(1000);
        b.Property(x => x.ParsedFilters).IsRequired().HasColumnType("nvarchar(Max)");

        b.HasIndex(x => x.UserId).HasDatabaseName("ix_saved_searches_user");
        b.HasIndex(x => new { x.IsActive, x.NotifyOnMatch })
         .HasDatabaseName("ix_saved_searches_active_notify");
    }
}