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

    /// <summary>
    /// Ensures the Qdrant collection and indexes exist, generates an embedding,
    /// and upserts the property directly — no duplicate check.
    /// Used by seed jobs to pre-populate the vector store.
    /// </summary>
    Task UpsertDirectAsync(
        PropertyDuplicateCheckRequest request,
        CancellationToken cancellationToken = default);
}