using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI.Application.DTOs
{
    public sealed record ExtractedLeaseField(
       string? Value,
       decimal ConfidenceScore,   // 0–100
       bool NeedsReview        // true when score < 80
   );
}
