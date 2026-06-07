using Document.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Document.Infrastructure.Persistence.Configurations;

public class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
    public void Configure(EntityTypeBuilder<Lead> b)
    {
        b.ToTable("leads");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.OwnerId).IsRequired();
        b.Property(x => x.PropertyId).IsRequired();
        b.Property(x => x.TenantId).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        b.Property(x => x.DealValue).HasColumnType("decimal(12,2)");
        b.Property(x => x.Notes).HasColumnType("nvarchar(max)");
        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.UpdatedAt).IsRequired();
        b.HasIndex(x => x.Status);
    }
}