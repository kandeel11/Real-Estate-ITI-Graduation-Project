// AI.Infrastructure/Services/InMemorySessionStore.cs

using AI.Application.Interfaces;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AI.Infrastructure.Services;

/// <summary>
/// In-memory session store — suitable for development.
/// Replace with RedisSessionStore in production.
/// </summary>
public sealed class InMemorySessionStore : ISessionStore
{
    private static readonly Dictionary<string, ChatHistory> _store = new();
    private static readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<ChatHistory> GetOrCreateAsync(
        string sessionId,
        string systemPrompt,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_store.TryGetValue(sessionId, out var existing))
                return existing;

            var history = new ChatHistory(systemPrompt);
            _store[sessionId] = history;
            return history;
        }
        finally { _lock.Release(); }
    }

    public async Task SaveAsync(
        string sessionId,
        ChatHistory history,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try { _store[sessionId] = history; }
        finally { _lock.Release(); }
    }

    public async Task DeleteAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try { _store.Remove(sessionId); }
        finally { _lock.Release(); }
    }
}