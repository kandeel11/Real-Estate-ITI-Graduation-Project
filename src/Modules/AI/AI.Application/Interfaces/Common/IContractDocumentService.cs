using AI.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI.Application.Interfaces.Common
{
    public interface IContractDocumentService
    {
        /// <summary>
        /// Ingests a contract PDF: extracts text, chunks it, generates embeddings,
        /// and upserts all vectors into Qdrant.
        /// </summary>
        Task<ContractIngestionResult> IngestAsync(
            Stream pdfStream,
            ContractIngestionRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes all vectors for a given contract from Qdrant.
        /// </summary>
        /// 
        Task DeleteAsync(Guid contractId, CancellationToken cancellationToken = default);

       
    }
}
