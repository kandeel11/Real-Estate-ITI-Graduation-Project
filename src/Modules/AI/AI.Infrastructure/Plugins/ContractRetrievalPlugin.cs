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
    // AI.Infrastructure/Plugins/ContractRetrievalPlugin.cs
    // Lower minScore — Arabic embeddings score lower than English

    [KernelFunction("search_contracts")]
    [Description("""
    Searches all stored contracts using semantic similarity.
    Use this when the user asks about contract terms, clauses,
    party names, prices, dates, or any contract information.
    Always call this before answering any contract-related question.
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
            collectionName: "contracts",
            topK: topK,
            minScore: 0.30);   // ✅ lowered from 0.70 — Arabic text scores lower

        if (results.Count == 0)
            return "No relevant contract clauses found for your query.";

        return FormatResults(results);
    }

    [KernelFunction("search_contract_by_id")]
    [Description("""
    Searches within a specific contract using its unique ID.
    Use this when the user refers to a specific contract by its ID.
    IMPORTANT: Always use this function when a contractId is provided in the message.
    """)]
    public async Task<string> SearchContractByIdAsync(
        [Description("The unique GUID identifier of the contract")]
    string contractId,

        [Description("The user's question about the contract")]
    string query,

        [Description("Maximum number of results to return (default 5)")]
    int topK = 5,

        Kernel? kernel = null)
    {
        if (!Guid.TryParse(contractId, out var guid))
            return $"Invalid contract ID: '{contractId}'";

        topK = Math.Clamp(topK, 1, 10);

        var results = await _retrieval.SearchAsync(
            query: query,
            collectionName: "contracts",
            topK: topK,
            minScore: 0.30,    // ✅ lowered — Arabic scores lower
            contractId: guid);

        if (results.Count == 0)
            return $"No results found in contract '{contractId}'. " +
                   $"Try search_contracts for a broader search.";

        return FormatResults(results);
    }

    [KernelFunction("search_contract_clauses_strict")]
    [Description("""
    Searches contracts with higher similarity threshold.
    Use for precise legal questions needing exact clause text.
    """)]
    public async Task<string> SearchContractClausesStrictAsync(
        [Description("The precise legal question to search for")]
    string query,

        Kernel? kernel = null)
    {
        var results = await _retrieval.SearchAsync(
            query: query,
            collectionName: "contracts",
            topK: 3,
            minScore: 0.55);   // ✅ lowered from 0.85

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