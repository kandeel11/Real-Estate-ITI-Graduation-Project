using Document.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Document.Infrastructure.Persistence.Configurations;

public class LeaseConfiguration : IEntityTypeConfiguration<Lease>
{
    public void Configure(EntityTypeBuilder<Lease> builder)
    {
        builder.ToTable("leases");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ContractFileUrl).IsRequired();
        builder.Property(x => x.ExtractionStatus).IsRequired().HasMaxLength(50);
        builder.Property(x => x.InstallmentAmount).HasColumnType("decimal(12,2)");
        builder.Property(x => x.TotalRentAmount).HasColumnType("decimal(12,2)");
        builder.Property(x => x.PaymentFrequency)
            .HasConversion<string>()
            .HasMaxLength(20);
        builder.Property(x => x.SecurityDeposit).HasColumnType("decimal(12,2)");
        builder.Property(x => x.ExtractionConfidence).HasColumnType("decimal(5,2)");
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasOne(x => x.Inquiry)
            .WithMany()
            .HasForeignKey(x => x.InquiryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Lead)
            .WithMany()
            .HasForeignKey(x => x.LeadId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}