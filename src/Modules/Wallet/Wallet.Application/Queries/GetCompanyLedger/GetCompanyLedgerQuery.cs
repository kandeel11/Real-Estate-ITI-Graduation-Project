using MediatR;
using Wallet.Application.Queries.GetLedger;

namespace Wallet.Application.Queries.GetCompanyLedger;

public record GetCompanyLedgerQuery(Guid CompanyId, int Page = 1, int PageSize = 20) : IRequest<PagedLedgerDto>;
