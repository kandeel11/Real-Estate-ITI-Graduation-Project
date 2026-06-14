using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI.Application.DTOs
{
    public sealed record DocumentExtractionResult(
    int TotalPages,
    IReadOnlyList<ExtractedPage> Pages
);
    public sealed record ExtractedPage(
    int PageNumber,
    string Text,
    IReadOnlyList<ExtractedTable> Tables    // bonus — Azure extracts tables too
);

    public sealed record ExtractedTable(
        int PageNumber,
        string MarkdownContent    // table rendered as markdown for better context
    );
}
