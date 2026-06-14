using AI.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI.Application.Interfaces.Common
{

    public interface IContractRetrievalService
    {
        /// <summary>
        /// Performs semantic search against Qdrant and returns the top-K chunks.
        /// </summary>
        Task<IReadOnlyList<ContractSearchResult>> SearchAsync(
            string query,
            string collectionName,
            int topK = 5,
            double minScore = 0.70,
            Guid? contractId = null,   // optional: scope to one contract
            CancellationToken cancellationToken = default);
    }
}
