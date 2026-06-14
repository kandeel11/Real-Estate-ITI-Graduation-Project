using Document.Domain.Enums;
using System.Xml.Linq;

namespace Document.Domain.Entities;

public class VerificationDocument
{
    public Guid Id { get; set; }
    public Guid VerificationRequestId { get; set; }  
    public DocumentType DocumentType { get; set; }
    public string FileUrl { get; set; } = null!;
    public int FileSizeKb { get; set; }
    public bool AiProcessed { get; set; } = false;
    public DateTime? AiProcessedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}