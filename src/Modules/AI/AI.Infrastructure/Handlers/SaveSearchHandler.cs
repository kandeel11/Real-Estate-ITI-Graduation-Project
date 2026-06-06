// AI.Infrastructure/Handlers/SaveSearchHandler.cs

using AI.Application.Commands.SaveSearch;
using AI.Application.DTOs;
using AI.Application.Interfaces;
using AI.Domain.Entities;
using AI.Infrastructure.Persistence.Contexts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AI.Infrastructure.Handlers;

public sealed class SaveSearchHandler
    : IRequestHandler<SaveSearchCommand, SavedSearchDto>
{
    private readonly ISavedSearchRepository _savedSearchRepo;
    private readonly AiDbContext _db;
    private readonly ILogger<SaveSearchHandler> _logger;

    public SaveSearchHandler(
        ISavedSearchRepository savedSearchRepo,
        AiDbContext db,
        ILogger<SaveSearchHandler> logger)
    {
        _savedSearchRepo = savedSearchRepo;
        _db = db;
        _logger = logger;
    }

    public async Task<SavedSearchDto> Handle(
        SaveSearchCommand command,
        CancellationToken ct)
    {
        // Load original query to get parsed filters
        var originalQuery = await _db.AiSearchQueries
            .FirstOrDefaultAsync(q => q.Id == command.QueryId, ct)
            ?? throw new KeyNotFoundException(
                $"Query '{command.QueryId}' not found.");

        var savedSearch = SavedSearch.Create(
            userId: command.UserId,
            name: command.Name,
            rawQuery: originalQuery.RawQuery,
            parsedFiltersJson: originalQuery.ParsedFilters,
            notifyOnMatch: command.NotifyOnMatch);

        await _savedSearchRepo.AddAsync(savedSearch, ct);

        _logger.LogInformation(
            "Saved search created | Id={Id} | Name={Name} | Notify={Notify}",
            savedSearch.Id, savedSearch.Name, savedSearch.NotifyOnMatch);

        var filters = JsonSerializer
            .Deserialize<ParsedSearchFilters>(savedSearch.ParsedFilters)!;

        return new SavedSearchDto(
            Id: savedSearch.Id,
            Name: savedSearch.Name,
            RawQuery: savedSearch.RawQuery,
            ParsedFilters: filters,
            NotifyOnMatch: savedSearch.NotifyOnMatch,
            IsActive: savedSearch.IsActive,
            CreatedAt: savedSearch.CreatedAt);
    }
}