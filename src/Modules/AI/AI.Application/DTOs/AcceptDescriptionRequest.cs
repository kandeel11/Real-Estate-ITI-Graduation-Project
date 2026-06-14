using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI.Application.DTOs
{
    // Both optional: omit them to accept the AI draft as-is; provide them only to save edits.
    public record AcceptDescriptionRequest(string? FinalTextEn = null, string? FinalTextAr = null);
}
