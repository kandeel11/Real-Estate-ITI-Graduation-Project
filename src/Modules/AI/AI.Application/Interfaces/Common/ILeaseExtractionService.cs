using AI.Application.DTOs;

namespace AI.Application.Interfaces.Common
{
    public interface ILeaseExtractionService
    {
        Task<ExtractedLeaseData> ExtractAsync(
            byte[] pdfBytes,
            CancellationToken cancellationToken = default);
    }
}
