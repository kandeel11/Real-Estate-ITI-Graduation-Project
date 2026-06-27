using MediatR;
using Wallet.Application.Interfaces;
using Wallet.Application.Queries.GetLedger;

namespace Wallet.Application.Queries.GetCompanyLedger;

public class GetCompanyLedgerHandler : IRequestHandler<GetCompanyLedgerQuery, PagedLedgerDto>
{
    private readonly IWalletRepository _repo;

    public GetCompanyLedgerHandler(IWalletRepository repo) => _repo = repo;

    public async Task<PagedLedgerDto> Handle(GetCompanyLedgerQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var skip = (page - 1) * pageSize;

        var total = await _repo.GetLedgerCountForCompanyAsync(request.CompanyId, ct);
        var items = await _repo.GetLedgerByCompanyIdAsync(request.CompanyId, skip, pageSize, ct);

        var dtos = items
            .Select(e => new LedgerEntryDto(e.Id, e.Type, e.Amount, e.Description, e.OccurredAt))
            .ToList();

        return new PagedLedgerDto(dtos, total, page, pageSize);
    }
}
