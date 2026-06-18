using Identity.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wallet.Application.Commands.RequestWithdrawal;
using Wallet.Application.Queries.GetLedger;
using Wallet.Application.Queries.GetWalletBalance;

namespace RealEstate.Host.Controllers;

[ApiController]
[Route("api/wallet")]
[Authorize(Roles = "owner,broker,company_manager,admin")]
public class WalletController(IMediator mediator, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance(CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId is null) return Unauthorized();

        var result = await mediator.Send(new GetWalletBalanceQuery(userId.Value), ct);
        return result is null
            ? Ok(new { ownerId = userId, balance = 0m, currency = "AED" })
            : Ok(result);
    }

    [HttpGet("ledger")]
    public async Task<IActionResult> GetLedger(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var userId = currentUser.UserId;
        if (userId is null) return Unauthorized();

        var result = await mediator.Send(new GetLedgerQuery(userId.Value, page, pageSize), ct);
        return Ok(result);
    }

    [HttpPost("withdrawals")]
    public async Task<IActionResult> RequestWithdrawal(
        [FromBody] RequestWithdrawalRequest body,
        CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId is null) return Unauthorized();

        var id = await mediator.Send(
            new RequestWithdrawalCommand(userId.Value, body.Amount, body.BankAccountDetails), ct);

        return Created($"/api/wallet/withdrawals/{id}", new { withdrawalId = id });
    }
}

public record RequestWithdrawalRequest(decimal Amount, string BankAccountDetails);
