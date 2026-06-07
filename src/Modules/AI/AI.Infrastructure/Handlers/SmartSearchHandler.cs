// AI.Infrastructure/Handlers/SmartSearchHandler.cs

using AI.Application.Commands.SmartSearch;
using AI.Application.DTOs;
using AI.Application.Interfaces;
using AI.Domain.Entities;
using AI.Domain.Enums;
using AI.Infrastructure.Persistence;
using AI.Infrastructure.Persistence.Contexts;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AI.Infrastructure.Handlers;

public sealed class SmartSearchHandler
    : IRequestHandler<SmartSearchCommand, SmartSearchResult>
{
    private readonly ISmartSearchService _parser;
    private readonly IPropertyFilterService _propertyFilter;
    private readonly AiDbContext _db;
    private readonly ILogger<SmartSearchHandler> _logger;

    public SmartSearchHandler(
        ISmartSearchService parser,
        IPropertyFilterService propertyFilter,
        AiDbContext db,
        ILogger<SmartSearchHandler> logger)
    {
        _parser = parser;
        _propertyFilter = propertyFilter;
        _db = db;
        _logger = logger;
    }

    public async Task<SmartSearchResult> Handle(
        SmartSearchCommand command,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Smart search | UserId={UserId} | Query={Query}",
            command.UserId, command.RawQuery);

        // ── Step 1: Claude parses query → structured filters ──────────────────
        var filters = await _parser.ParseQueryAsync(command.RawQuery, ct);

        // ── Step 2: Search properties using filters ───────────────────────────
        var (items, total) = await _propertyFilter.SearchAsync(
            filters, command.Page, command.PageSize, ct);

        // ── Step 3: Log query to ai.ai_search_queries ─────────────────────────
        var query = AiSearchQuery.Create(
            userId: command.UserId,
            rawQuery: command.RawQuery,
            language: filters.DetectedLanguage,
            parsedFiltersJson: JsonSerializer.Serialize(filters),
            resultCount: total,
            tokensUsed: 150,    // approximate — update with actual token count
            confidence: filters.Confidence);

        await _db.AiSearchQueries.AddAsync(query, ct);

        // ── Step 4: Log usage to ai.ai_usage_logs ─────────────────────────────
        var usageLog = AiUsageLog.Create(
            userId: command.UserId ?? Guid.Empty,      
            companyId: null,                          
            subscriptionId: Guid.Empty,               
            featureType: AiFeatureType.SmartSearch,    
            tokensUsed: 150,
            requestPayload: JsonSerializer.Serialize(filters),
            responseSummary: $"Found {total} properties",
            success: true,
            errorMessage: null
        );

        await _db.AiUsageLogs.AddAsync(usageLog, ct);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Smart search complete | QueryId={Id} | Results={Count} | " +
            "City={City} Type={Type} Beds={Beds}",
            query.Id, total, filters.City, filters.PropertyType, filters.Bedrooms);

        return new SmartSearchResult(
            QueryId: query.Id,
            ParsedQuery: filters,
            TotalResults: total,
            Properties: items,
            Page: command.Page,
            PageSize: command.PageSize,
            TokensUsed: 150);
    }
}