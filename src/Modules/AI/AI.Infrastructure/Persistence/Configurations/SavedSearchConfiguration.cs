using AI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AI.Infrastructure.Persistence.Configurations
{
    public class SavedSearchConfiguration : IEntityTypeConfiguration<SavedSearch>
    {
        public void Configure(EntityTypeBuilder<SavedSearch> builder)
        {
            builder.ToTable("saved_searches", "ai");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

            builder.Property(x => x.UserId).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(200).IsRequired();

            builder.Property(x => x.Filters).IsRequired();

            builder.Property(x => x.NotifyOnMatch).HasDefaultValue(false);

            builder.HasQueryFilter(x => x.DeletedAt == null);
        }
    }
}
