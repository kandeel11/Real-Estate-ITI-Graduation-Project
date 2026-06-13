using AI.Application.Interfaces;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AI.Infrastructure.Services;

/// <summary>
/// In-memory session store — suitable for development.
/// Replace with RedisSessionStore in production.
/// </summary>
public sealed class InMemorySessionStore : ISessionStore
{
    // 💡 التعديل 1: تغيير نوع المفتاح (Key) في القاموس لـ Guid بدل string
    private static readonly Dictionary<Guid, ChatHistory> _store = new();
    private static readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<ChatHistory> GetOrCreateAsync(
        Guid sessionId,
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

    // 💡 التعديل 2: تغيير الـ sessionId لـ Guid
    public async Task SaveAsync(
        Guid sessionId,
        ChatHistory history,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try { _store[sessionId] = history; }
        finally { _lock.Release(); }
    }

    // 💡 التعديل 3: تغيير الـ sessionId لـ Guid
    public async Task DeleteAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try { _store.Remove(sessionId); }
        finally { _lock.Release(); }
    }
}