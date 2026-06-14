using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI.Application.DTOs
{
    public sealed record ContractChunk(
    Guid ChunkId,
    Guid ContractId,
    int PageNumber,
    int ChunkIndex,
    string Text
);
}
