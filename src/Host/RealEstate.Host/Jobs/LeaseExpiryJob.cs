using Document.Application.Commands.Lease.ExpireDueLeases;
using MediatR;

namespace RealEstate.Host.Jobs;

/// <summary>
/// Daily Hangfire job: expires leases past their EndDate and returns their
/// properties to the market. Idempotent, so re-runs are safe.
/// </summary>
public class LeaseExpiryJob
{
    private readonly IMediator _mediator;
    private readonly ILogger<LeaseExpiryJob> _logger;

    public LeaseExpiryJob(IMediator mediator, ILogger<LeaseExpiryJob> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        var expired = await _mediator.Send(new ExpireDueLeasesCommand());
        if (expired > 0)
            _logger.LogInformation("Lease expiry sweep ended {Count} due lease(s).", expired);
    }
}
