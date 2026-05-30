using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI.Application.DTOs
{
    public sealed record ContractIngestionRequest(
     Guid ContractId,
     string CollectionName,       // e.g. "contracts"
     string FileName,
     int ChunkSize = 512,   // tokens per chunk
     int ChunkOverlap = 64     // overlap between consecutive chunks
 );
}
