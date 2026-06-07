using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI.Application.DTOs
{
    public sealed record ContractIngestionResult(
    Guid ContractId,
    int TotalPages,
    int TotalChunks,
    int UpsertedVectors,
    TimeSpan Duration
);
}
