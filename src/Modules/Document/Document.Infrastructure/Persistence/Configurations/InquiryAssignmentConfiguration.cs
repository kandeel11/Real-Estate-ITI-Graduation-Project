using Document.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Document.Infrastructure.Persistence.Configurations;

public class InquiryAssignmentConfiguration : IEntityTypeConfiguration<InquiryAssignment>
{
    public void Configure(EntityTypeBuilder<InquiryAssignment> b)
    {
        b.ToTable("inquiry_assignments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.AssignedToId).IsRequired();
        b.Property(x => x.AssignedById).IsRequired();
        b.Property(x => x.IsAccepted).HasDefaultValue(false);
        b.Property(x => x.CreatedAt).IsRequired();
    }
}