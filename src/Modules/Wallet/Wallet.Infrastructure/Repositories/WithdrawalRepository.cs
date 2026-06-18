using Microsoft.EntityFrameworkCore;
using Wallet.Application.Interfaces;
using Wallet.Domain.Entities;
using Wallet.Infrastructure.Persistence;

namespace Wallet.Infrastructure.Repositories;

public class WithdrawalRepository : IWithdrawalRepository
{
    private readonly WalletDbContext _context;

    public WithdrawalRepository(WalletDbContext context) => _context = context;

    public async Task<Guid> CreateAsync(WithdrawalRequest request, CancellationToken ct = default)
    {
        await _context.WithdrawalRequests.AddAsync(request, ct);
        await _context.SaveChangesAsync(ct);
        return request.Id;
    }

    public async Task<List<WithdrawalRequest>> GetByOwnerIdAsync(Guid ownerId, CancellationToken ct = default)
        => await _context.WithdrawalRequests
            .Where(w => w.OwnerId == ownerId)
            .OrderByDescending(w => w.RequestedAt)
            .ToListAsync(ct);

    public async Task<List<WithdrawalRequest>> GetAllAsync(CancellationToken ct = default)
        => await _context.WithdrawalRequests
            .OrderByDescending(w => w.RequestedAt)
            .ToListAsync(ct);

    public async Task<WithdrawalRequest?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.WithdrawalRequests.FirstOrDefaultAsync(w => w.Id == id, ct);

    public async Task UpdateAsync(WithdrawalRequest request, CancellationToken ct = default)
    {
        _context.WithdrawalRequests.Update(request);
        await _context.SaveChangesAsync(ct);
    }
}
