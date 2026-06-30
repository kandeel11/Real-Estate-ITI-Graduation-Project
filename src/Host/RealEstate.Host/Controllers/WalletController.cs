using Identity.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wallet.Application.Commands.ApproveWithdrawal;
using Wallet.Application.Commands.RejectWithdrawal;
using Wallet.Application.Commands.RequestWithdrawal;
using Wallet.Application.Queries.GetAllWithdrawals;
using Wallet.Application.Queries.GetLedger;
using Wallet.Application.Queries.GetOwnerWithdrawals;
using Wallet.Application.Queries.GetCompanyWalletBalance;
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

        if (currentUser.IsInRole("company_manager") && currentUser.CompanyId.HasValue)
        {
            var result = await mediator.Send(new GetCompanyWalletBalanceQuery(currentUser.CompanyId.Value), ct);
            return result is null
                ? Ok(new { companyId = currentUser.CompanyId, balance = 0m, currency = "AED" })
                : Ok(result);
        }

        var ownerResult = await mediator.Send(new GetWalletBalanceQuery(userId.Value), ct);
        return ownerResult is null
            ? Ok(new { ownerId = userId, balance = 0m, currency = "AED" })
            : Ok(ownerResult);
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

        try
        {
            var id = await mediator.Send(
                new RequestWithdrawalCommand(userId.Value, body.Amount, body.BankAccountDetails), ct);
            return Created($"/api/wallet/withdrawals/{id}", new { withdrawalId = id });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("withdrawals")]
    public async Task<IActionResult> GetWithdrawals(CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId is null) return Unauthorized();

        var result = await mediator.Send(new GetOwnerWithdrawalsQuery(userId.Value), ct);
        return Ok(result);
    }

    [HttpGet("admin/withdrawals")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAllWithdrawals(CancellationToken ct)
    {
        var result = await mediator.Send(new GetAllWithdrawalsQuery(), ct);
        return Ok(result);
    }

    [HttpPut("admin/withdrawals/{id:guid}/approve")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> ApproveWithdrawal(Guid id, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new ApproveWithdrawalCommand(id), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPut("admin/withdrawals/{id:guid}/reject")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> RejectWithdrawal(Guid id, [FromBody] RejectWithdrawalRequest body, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new RejectWithdrawalCommand(id, body.Notes), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }
}

public record RequestWithdrawalRequest(decimal Amount, string BankAccountDetails);
public record RejectWithdrawalRequest(string? Notes = null);
