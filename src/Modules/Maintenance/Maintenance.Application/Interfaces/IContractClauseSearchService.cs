namespace Maintenance.Application.Interfaces;

public interface IContractClauseSearchService
{
    /// <summary>
    /// Searches the 'contracts' Qdrant collection for clauses relevant to the
    /// given issue, filtered to only the contract for the specified lease.
    /// Returns clause texts ordered by semantic similarity (most relevant first).
    /// </summary>
    Task<IReadOnlyList<string>> SearchAsync(
        string leaseId,
        string issueDescription,
        int topK = 5,
        CancellationToken ct = default);
}
