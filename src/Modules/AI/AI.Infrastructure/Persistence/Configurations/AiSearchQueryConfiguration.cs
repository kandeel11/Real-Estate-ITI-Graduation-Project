using AI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AI.Infrastructure.Persistence.Configurations
{
    public class AiSearchQueryConfiguration : IEntityTypeConfiguration<AiSearchQuery>
    {
        public void Configure(EntityTypeBuilder<AiSearchQuery> builder)
        {
            builder.ToTable("ai_search_queries", "ai");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

            builder.Property(x => x.UserId).IsRequired(false);
            builder.Property(x => x.RawQuery).IsRequired();

            builder.Property(x => x.ParsedFilters).IsRequired();

            builder.Property(x => x.SessionId).HasMaxLength(255);
            builder.Property(x => x.IpAddress).HasMaxLength(50);

            builder.HasQueryFilter(x => x.DeletedAt == null);
        }
    }
}
