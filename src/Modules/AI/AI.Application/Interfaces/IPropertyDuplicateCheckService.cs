// AI.Application/Interfaces/IPropertyDuplicateCheckService.cs

using AI.Application.DTOs;

namespace AI.Application.Interfaces;

public interface IPropertyDuplicateCheckService
{
    /// <summary>
    /// 1. Build searchable text from property fields
    /// 2. Embed with OpenAI
    /// 3. Search Qdrant for similar properties
    /// 4. Claude decides if truly duplicate
    /// 5. Upsert new property into Qdrant
    /// </summary>
    Task<DuplicateCheckResult> CheckAsync(
        PropertyDuplicateCheckRequest request,
        CancellationToken cancellationToken = default);
}