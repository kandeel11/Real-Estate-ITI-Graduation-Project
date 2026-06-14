using Document.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Document.Infrastructure.Persistence.Configurations;

public class VerificationDocumentConfiguration : IEntityTypeConfiguration<VerificationDocument>
{
    public void Configure(EntityTypeBuilder<VerificationDocument> b)
    {
        b.ToTable("verification_documents");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.VerificationRequestId).IsRequired();
        b.Property(x => x.DocumentType).HasConversion<string>().HasMaxLength(100).IsRequired();
        b.Property(x => x.FileUrl).HasMaxLength(500).IsRequired();
        b.Property(x => x.FileSizeKb).IsRequired();
        b.Property(x => x.AiProcessed).HasDefaultValue(false);
        b.Property(x => x.CreatedAt).IsRequired();
    }
}