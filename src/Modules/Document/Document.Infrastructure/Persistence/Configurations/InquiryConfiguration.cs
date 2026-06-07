using Document.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Document.Infrastructure.Persistence.Configurations;

public class InquiryConfiguration : IEntityTypeConfiguration<Domain.Entities.Inquiry>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Inquiry> b)
    {
        b.ToTable("inquiries");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.PropertyId).IsRequired();
        b.Property(x => x.TenantId).IsRequired();
        b.Property(x => x.OwnerId).IsRequired();
        b.Property(x => x.Message).HasColumnType("nvarchar(max)").IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.UpdatedAt).IsRequired();
        b.HasIndex(x => x.PropertyId);
        b.HasIndex(x => x.TenantId);
        b.HasIndex(x => x.Status);
        b.HasMany(x => x.Assignments).WithOne(x => x.Inquiry)
            .HasForeignKey(x => x.InquiryId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Leads).WithOne(x => x.Inquiry)
            .HasForeignKey(x => x.InquiryId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Conversations).WithOne(x => x.Inquiry)
            .HasForeignKey(x => x.InquiryId).OnDelete(DeleteBehavior.SetNull);
    }
}