using AI.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;

namespace AI.Infrastructure.Services;

/// <summary>
/// Production-ready session store backed by Redis (Memurai on Windows).
/// Replaces InMemorySessionStore — sessions survive restarts and scale across instances.
/// Sliding 30-minute expiry: idle sessions expire automatically, active ones stay warm.
/// </summary>
public sealed class RedisSessionStore : ISessionStore
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisSessionStore> _logger;

    private static readonly TimeSpan _sliding = TimeSpan.FromMinutes(30);

    // Compact DTO — we only need role + text for round-trip storage
    private sealed record StoredMessage(string Role, string Content);

    public RedisSessionStore(IDistributedCache cache, ILogger<RedisSessionStore> logger)
    {
        _cache  = cache;
        _logger = logger;
    }

    public async Task<ChatHistory> GetOrCreateAsync(
        Guid sessionId,
        string systemPrompt,
        CancellationToken cancellationToken = default)
    {
        var key  = Key(sessionId);
        var json = await _cache.GetStringAsync(key, cancellationToken);

        if (json is null)
        {
            _logger.LogDebug("[Session] New session {Id}", sessionId);
            return new ChatHistory(systemPrompt);
        }

        var records = JsonSerializer.Deserialize<List<StoredMessage>>(json);
        if (records is null || records.Count == 0)
            return new ChatHistory(systemPrompt);

        var history = new ChatHistory();
        foreach (var msg in records)
            history.Add(new ChatMessageContent(new AuthorRole(msg.Role), msg.Content));

        _logger.LogDebug("[Session] Restored {Id} — {N} messages", sessionId, records.Count);
        return history;
    }

    public async Task SaveAsync(
        Guid sessionId,
        ChatHistory history,
        CancellationToken cancellationToken = default)
    {
        var records = history
            .Select(m => new StoredMessage(m.Role.Label, m.Content ?? string.Empty))
            .ToList();

        var json = JsonSerializer.Serialize(records);
        var opts = new DistributedCacheEntryOptions { SlidingExpiration = _sliding };

        await _cache.SetStringAsync(Key(sessionId), json, opts, cancellationToken);
        _logger.LogDebug("[Session] Saved {Id} — {N} messages", sessionId, records.Count);
    }

    public async Task DeleteAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(Key(sessionId), cancellationToken);
        _logger.LogDebug("[Session] Deleted {Id}", sessionId);
    }

    private static string Key(Guid id) => $"ai:session:{id}";
}
