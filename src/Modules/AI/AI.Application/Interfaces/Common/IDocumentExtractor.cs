using AI.Application.DTOs;

namespace AI.Application.Interfaces.Common
{
    public interface IDocumentExtractor
    {
        /// <summary>
        /// Extracts structured pages from a PDF stream.
        /// Works for both text-based and image/scanned PDFs.
        /// </summary>
        Task<DocumentExtractionResult> ExtractAsync(
            Stream pdfStream,
            CancellationToken cancellationToken = default);
    }
}
