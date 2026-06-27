using MediatR;
using Microsoft.AspNetCore.Http;

namespace Document.Application.Commands.Lease.UploadLeaseContract;

public record UploadLeaseContractCommand(
    Guid LeadId,
    Guid OwnerId,
    IFormFile File,
    Guid? BrokerId = null) : IRequest<UploadLeaseResponse>;

public record UploadLeaseResponse(
    Guid LeaseId,
    string Status,
    ExtractedLeaseData? ExtractedData);

public record ExtractedLeaseData(
    decimal? InstallmentAmount,
    string? PaymentFrequency,
    decimal? TotalRentAmount,
    int? DurationInMonths,
    decimal? SecurityDeposit,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int? PaymentDayOfMonth,
    decimal? Confidence);
