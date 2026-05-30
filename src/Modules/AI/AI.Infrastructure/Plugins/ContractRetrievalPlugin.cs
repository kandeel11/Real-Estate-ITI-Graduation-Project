// AI.Infrastructure/Plugins/ContractRetrievalPlugin.cs

using AI.Application.DTOs;
using AI.Application.Interfaces.Common;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace AI.Infrastructure.Plugins;

/// <summary>
/// Semantic Kernel plugin that gives the AI Agent the ability to search
/// contract clauses using natural language.
///
/// The Kernel calls these functions automatically when it decides
/// retrieval is needed — no manual orchestration required.
/// </summary>
public sealed class ContractRetrievalPlugin
{
    private const string DefaultCollection = "contracts";

    private readonly IContractRetrievalService _retrieval;
    private readonly ILogger<ContractRetrievalPlugin> _logger;

    public ContractRetrievalPlugin(
        IContractRetrievalService retrieval,
        ILogger<ContractRetrievalPlugin> logger)
    {
        _retrieval = retrieval;
        _logger = logger;
    }

    // ────────────────────────────────────────────────────────────────────────
    // Function 1 — General search across ALL contracts
    // ────────────────────────────────────────────────────────────────────────

    [KernelFunction("search_contracts")]
    [Description("""
        Searches all stored contracts using semantic similarity.
        Use this when the user asks a general question about contract terms,
        clauses, obligations, or conditions without specifying a contract.
        Returns the most relevant text excerpts ranked by similarity score.
        """)]
    public async Task<string> SearchContractsAsync(
        [Description("The user's natural language question or search query")]
        string query,

        [Description("Maximum number of results to return (default 5, max 10)")]
        int topK = 5,

        Kernel? kernel = null)
    {
        topK = Math.Clamp(topK, 1, 10);

        var results = await _retrieval.SearchAsync(
            query: query,
            collectionName: DefaultCollection,
            topK: topK,
            minScore: 0.70);

        if (results.Count == 0)
            return "No relevant contract clauses found for your query.";

        return FormatResults(results);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Function 2 — Targeted search within ONE specific contract
    // ────────────────────────────────────────────────────────────────────────

    [KernelFunction("search_contract_by_id")]
    [Description("""
        Searches within a specific contract using its unique ID.
        Use this when the user refers to a particular contract by its ID
        and asks about its specific clauses, terms, or obligations.
        """)]
    public async Task<string> SearchContractByIdAsync(
        [Description("The unique GUID identifier of the contract to search within")]
        string contractId,

        [Description("The user's natural language question or search query")]
        string query,

        [Description("Maximum number of results to return (default 5, max 10)")]
        int topK = 5,

        Kernel? kernel = null)
    {
        if (!Guid.TryParse(contractId, out var guid))
            return $"Invalid contract ID format: '{contractId}'. Expected a valid GUID.";

        topK = Math.Clamp(topK, 1, 10);

        var results = await _retrieval.SearchAsync(
            query: query,
            collectionName: DefaultCollection,
            topK: topK,
            minScore: 0.70,
            contractId: guid);

        if (results.Count == 0)
            return $"No relevant clauses found in contract '{contractId}' for your query.";

        return FormatResults(results);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Function 3 — Strict legal search (higher similarity threshold)
    // ────────────────────────────────────────────────────────────────────────

    [KernelFunction("search_contract_clauses_strict")]
    [Description("""
        Searches contracts with a higher similarity threshold (0.85+).
        Use this for precise legal questions where accuracy is critical,
        such as exact obligation wording, penalty clauses, or legal definitions.
        Prefer this over search_contracts when the user needs exact clause text.
        """)]
    public async Task<string> SearchContractClausesStrictAsync(
        [Description("The precise legal question or clause type to search for")]
        string query,

        Kernel? kernel = null)
    {
        var results = await _retrieval.SearchAsync(
            query: query,
            collectionName: DefaultCollection,
            topK: 3,
            minScore: 0.85);   // strict threshold

        if (results.Count == 0)
            return "No highly relevant clauses found. Try search_contracts for broader results.";

        return FormatResults(results);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Shared formatter
    // ────────────────────────────────────────────────────────────────────────

    private static string FormatResults(IReadOnlyList<ContractSearchResult> results)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Found {results.Count} relevant clause(s):\n");

        for (var i = 0; i < results.Count; i++)
        {
            var r = results[i];
            sb.AppendLine($"[{i + 1}] Contract: {r.ContractId}");
            sb.AppendLine($"    Page: {r.PageNumber} | Chunk: {r.ChunkIndex} | Score: {r.Score:P1}");
            sb.AppendLine($"    Text: {r.Text}");
            sb.AppendLine();
        }

        return sb.ToString();
    }
}