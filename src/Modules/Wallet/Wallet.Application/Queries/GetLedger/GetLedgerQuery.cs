using MediatR;
using Wallet.Domain.Enums;

namespace Wallet.Application.Queries.GetLedger;

public record GetLedgerQuery(Guid OwnerId, int Page = 1, int PageSize = 20) : IRequest<PagedLedgerDto>;

public record LedgerEntryDto(Guid Id, LedgerEntryType Type, decimal Amount, string Description, DateTime OccurredAt);

public record PagedLedgerDto(List<LedgerEntryDto> Items, int TotalCount, int Page, int PageSize);
