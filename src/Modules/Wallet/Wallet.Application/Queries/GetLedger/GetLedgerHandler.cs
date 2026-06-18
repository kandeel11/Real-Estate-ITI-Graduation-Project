using MediatR;
using Wallet.Application.Interfaces;

namespace Wallet.Application.Queries.GetLedger;

public class GetLedgerHandler : IRequestHandler<GetLedgerQuery, PagedLedgerDto>
{
    private readonly IWalletRepository _repo;

    public GetLedgerHandler(IWalletRepository repo) => _repo = repo;

    public async Task<PagedLedgerDto> Handle(GetLedgerQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var skip = (page - 1) * pageSize;

        var total = await _repo.GetLedgerCountAsync(request.OwnerId, cancellationToken);
        var items = await _repo.GetLedgerByOwnerIdAsync(request.OwnerId, skip, pageSize, cancellationToken);

        var dtos = items
            .Select(e => new LedgerEntryDto(e.Id, e.Type, e.Amount, e.Description, e.OccurredAt))
            .ToList();

        return new PagedLedgerDto(dtos, total, page, pageSize);
    }
}
