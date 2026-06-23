using MediatR;
using Microsoft.Extensions.Logging;
using Payment.Application.Helpers;
using Payment.Application.Interfaces;
using PaymentEntity = Payment.Domain.Entities.Payment;

namespace Payment.Application.Features.Commands.GenerateLeasePayment;

/// <summary>
/// Generates the payment for the current period of a specific lease from the real database.
/// Use when the monthly job didn't run or a lease's payment was skipped.
/// </summary>
public sealed class GenerateLeasePaymentHandler
    : IRequestHandler<GenerateLeasePaymentCommand, GenerateLeasePaymentResult>
{
    private readonly IPaymentRepository _repository;
    private readonly ILeaseService _leaseService;
    private readonly ILogger<GenerateLeasePaymentHandler> _logger;

    public GenerateLeasePaymentHandler(
        IPaymentRepository repository,
        ILeaseService leaseService,
        ILogger<GenerateLeasePaymentHandler> logger)
    {
        _repository = repository;
        _leaseService = leaseService;
        _logger = logger;
    }

    public async Task<GenerateLeasePaymentResult> Handle(
        GenerateLeasePaymentCommand request,
        CancellationToken cancellationToken)
    {
        var lease = await _leaseService.GetLeaseByIdAsync(request.LeaseId, cancellationToken);

        if (lease is null)
            throw new KeyNotFoundException($"Lease {request.LeaseId} not found or not active.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Guard: not yet started
        if (today < lease.StartDate)
            return new GenerateLeasePaymentResult(
                Generated: false,
                PaymentId: null,
                DueDate: lease.StartDate,
                Message: $"Lease has not started yet. First payment is due on {lease.StartDate:yyyy-MM-dd}.");

        // Guard: lease expired
        if (today > lease.EndDate)
            return new GenerateLeasePaymentResult(
                Generated: false,
                PaymentId: null,
                DueDate: lease.EndDate,
                Message: $"Lease ended on {lease.EndDate:yyyy-MM-dd}. No new payments can be generated.");

        // Find the next due date respecting the lease's payment frequency and period structure
        var dueDate = LeasePaymentHelper.ComputeNextDueDate(
            lease.StartDate, lease.PaymentFrequency, lease.PaymentDayOfMonth, today);

        bool exists = await _repository.ExistsForLeaseAndMonthAsync(
            request.LeaseId, dueDate, cancellationToken);

        if (exists)
        {
            _logger.LogInformation(
                "Payment already exists for lease {LeaseId} period {DueDate}",
                request.LeaseId, dueDate);

            return new GenerateLeasePaymentResult(
                Generated: false,
                PaymentId: null,
                DueDate: dueDate,
                Message: $"Payment for {dueDate:MMMM yyyy} already exists.");
        }

        var payment = PaymentEntity.CreateRent(
            leaseId: lease.LeaseId,
            propertyId: lease.PropertyId,
            tenantId: lease.TenantId,
            landlordId: lease.LandlordId,
            amount: lease.InstallmentAmount,
            dueDate: dueDate,
            notes: $"Rent for {dueDate:MMMM yyyy}"
        );

        await _repository.AddAsync(payment, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Generated payment {PaymentId} for lease {LeaseId} due {DueDate}",
            payment.Id, request.LeaseId, dueDate);

        return new GenerateLeasePaymentResult(
            Generated: true,
            PaymentId: payment.Id,
            DueDate: dueDate,
            Message: $"Payment generated for {dueDate:MMMM yyyy}.");
    }
}
