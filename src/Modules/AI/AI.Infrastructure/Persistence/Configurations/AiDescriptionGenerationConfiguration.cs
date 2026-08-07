using AI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AI.Infrastructure.Persistence.Configurations
{
    public class AiDescriptionGenerationConfiguration : IEntityTypeConfiguration<AiDescriptionGeneration>
    {
        public void Configure(EntityTypeBuilder<AiDescriptionGeneration> builder)
        {
            builder.ToTable("ai_description_generations", "ai");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

            builder.Property(x => x.PropertyId).IsRequired();
            builder.Property(x => x.RequestedById).IsRequired();

            builder.Property(x => x.PromptText).IsRequired();
            builder.Property(x => x.GeneratedTextEn).IsRequired();
            builder.Property(x => x.GeneratedTextAr).IsRequired();

            builder.Property(x => x.ModelVersion).HasMaxLength(50);
            builder.Property(x => x.IsAccepted).HasDefaultValue(false);

            builder.HasQueryFilter(x => x.DeletedAt == null);

        }
    }
}
