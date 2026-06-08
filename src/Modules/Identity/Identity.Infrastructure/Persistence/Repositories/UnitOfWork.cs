// Identity.Infrastructure/Persistence/UnitOfWork.cs

using Identity.Application.Interfaces.Common;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly IdentityDbContext _db;
    private readonly ILogger<UnitOfWork> _logger;

    public UnitOfWork(IdentityDbContext db, ILogger<UnitOfWork> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<int> CommitAsync(CancellationToken ct = default)
    {
        try
        {
            var affected = await _db.SaveChangesAsync(ct);

            _logger.LogDebug("UnitOfWork committed | Affected={Count}", affected);
            return affected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UnitOfWork commit failed.");
            throw;
        }
    }


    //public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(
    //    CancellationToken ct = default)
    //{
    //    var tx = await _db.Database.BeginTransactionAsync(ct);
    //    return new EfTransaction(tx);
}

    // ── Inner adapter for EF Core transactions ──────────────────────────
    //private sealed class EfTransaction : IUnitOfWorkTransaction
    //{
    //    private readonly IDbContextTransaction _tx;

    //    public EfTransaction(IDbContextTransaction tx) => _tx = tx;

    //    public Task CommitAsync(CancellationToken ct = default) => _tx.CommitAsync(ct);
    //    public Task RollbackAsync(CancellationToken ct = default) => _tx.RollbackAsync(ct);

    //    public async ValueTask DisposeAsync() => await _tx.DisposeAsync();
    //}
