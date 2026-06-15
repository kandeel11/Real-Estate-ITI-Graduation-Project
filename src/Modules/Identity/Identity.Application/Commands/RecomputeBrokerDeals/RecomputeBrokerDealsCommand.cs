using Identity.Application.Interfaces;
using MediatR;

namespace Identity.Application.Commands.RecomputeBrokerDeals;

/// <summary>
/// Sets each broker's ClosedDealsCount to an absolute value (keyed by the broker's
/// user id). Idempotent — used by the one-time backfill to rebuild the metric from
/// won leads. Brokers absent from the map are left untouched (they already have 0).
/// </summary>
public record RecomputeBrokerDealsCommand(IReadOnlyDictionary<Guid, int> CountsByUserId)
    : IRequest<int>;

public class RecomputeBrokerDealsHandler : IRequestHandler<RecomputeBrokerDealsCommand, int>
{
    private readonly IBrokerRepository _brokers;

    public RecomputeBrokerDealsHandler(IBrokerRepository brokers) => _brokers = brokers;

    public async Task<int> Handle(RecomputeBrokerDealsCommand request, CancellationToken ct)
    {
        var updated = 0;
        foreach (var (userId, count) in request.CountsByUserId)
        {
            var broker = await _brokers.GetByUserIdAsync(userId, ct);
            if (broker is null) continue; // not a broker (e.g. an owner) — skip
            broker.SetClosedDealsCount(count);
            updated++;
        }

        if (updated > 0)
            await _brokers.SaveChangesAsync(ct);

        return updated;
    }
}
