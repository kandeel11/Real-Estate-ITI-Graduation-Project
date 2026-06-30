using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.Persistence.Configurations;

public sealed class LeaseScheduleConfigConfiguration : IEntityTypeConfiguration<LeaseScheduleConfig>
{
    public void Configure(EntityTypeBuilder<LeaseScheduleConfig> builder)
    {
        builder.ToTable("lease_schedule_configs", "payment");

        builder.HasKey(x => x.LeaseId);
        builder.Property(x => x.LeaseId).ValueGeneratedNever();

        builder.Property(x => x.PropertyId).IsRequired();
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.LandlordId).IsRequired();
        builder.Property(x => x.InstallmentAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.PaymentFrequency).HasMaxLength(20).IsRequired();
        builder.Property(x => x.PaymentDayOfMonth).IsRequired();
        builder.Property(x => x.StartDate).IsRequired();
        builder.Property(x => x.EndDate).IsRequired();
    }
}
