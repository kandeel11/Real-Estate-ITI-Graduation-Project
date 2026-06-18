using System.Threading.Channels;

namespace Payment.Infrastructure.Outbox;

/// <summary>
/// Singleton signal between OutboxWriter (writes) and OutboxPublisherJob (reads).
/// Lets the publisher wake up immediately when a message is committed instead of
/// polling on a fixed timer. Capacity-1 bounded channel: if a second signal arrives
/// before the publisher wakes, it is silently dropped — one pending notification is
/// enough to trigger a sweep.
/// </summary>
public sealed class OutboxProcessingSignal
{
    private readonly Channel<byte> _channel = Channel.CreateBounded<byte>(
        new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false
        });

    public void Notify() => _channel.Writer.TryWrite(0);

    /// <summary>
    /// Waits until notified or <paramref name="timeout"/> elapses (safety-net poll).
    /// Returns without throwing when <paramref name="ct"/> is cancelled.
    /// </summary>
    public async Task WaitAsync(TimeSpan timeout, CancellationToken ct)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
        linked.CancelAfter(timeout);
        try
        {
            await _channel.Reader.ReadAsync(linked.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // timeout elapsed — proceed with fallback sweep
        }
    }
}
