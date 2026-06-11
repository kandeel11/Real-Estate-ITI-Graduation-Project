// AI.Application/Interfaces/ISessionStore.cs

using Microsoft.SemanticKernel.ChatCompletion;

namespace AI.Application.Interfaces;

/// <summary>
/// Abstraction for storing per-session ChatHistory.
/// Swap implementations: InMemory → Redis → SQL without touching handlers.
/// </summary>
public interface ISessionStore
{
    Task<ChatHistory> GetOrCreateAsync(
        Guid sessionId,
        string systemPrompt,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        Guid sessionId,
        ChatHistory history,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);
}