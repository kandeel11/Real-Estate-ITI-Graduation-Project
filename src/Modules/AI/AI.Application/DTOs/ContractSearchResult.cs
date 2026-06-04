using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI.Application.DTOs
{
    public sealed record ContractSearchResult(
    Guid ContractId,
    int PageNumber,
    int ChunkIndex,
    string Text,
    double Score          // cosine similarity score 0–1
);
}
