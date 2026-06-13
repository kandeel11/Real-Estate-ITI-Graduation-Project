using Document.Application.DTOs;
using Document.Application.Interfaces;
using Document.Domain.Enums;
using MediatR;
using RealEstate.Shared.Kernel.Shared.Contracts;

namespace Document.Application.Commands.Inquiry;

public record CreateInquiryCommand(
    Guid PropertyId,
    Guid TenantId,
    Guid OwnerId,
    Guid? CompanyId,
    string Message
) : IRequest<InquiryResponse>;

public class CreateInquiryCommandHandler : IRequestHandler<CreateInquiryCommand, InquiryResponse>
{
    private readonly IInquiryRepository _inquiries;
    private readonly INotificationService _notifications;
    private readonly ISubscriptionLimitChecker _limitChecker;

    public CreateInquiryCommandHandler(
        IInquiryRepository inquiries,
        INotificationService notifications,
        ISubscriptionLimitChecker limitChecker)
    {
        _inquiries = inquiries;
        _notifications = notifications;
        _limitChecker = limitChecker;
    }

    public async Task<InquiryResponse> Handle(CreateInquiryCommand cmd, CancellationToken ct)
    {
        // Enforce the tenant plan's MaxInquiriesPerMonth
        var (allowed, reason) = await _limitChecker.CheckInquiryLimitAsync(cmd.TenantId, ct);
        if (!allowed)
            throw new InvalidOperationException(reason);

        var inquiry = new Domain.Entities.Inquiry
        {
            Id = Guid.NewGuid(),
            PropertyId = cmd.PropertyId,
            TenantId = cmd.TenantId,
            OwnerId = cmd.OwnerId,
            CompanyId = cmd.CompanyId,
            Message = cmd.Message,
            Status = InquiryStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _inquiries.AddAsync(inquiry, ct);
        await _limitChecker.IncrementInquiryUsageAsync(cmd.TenantId, ct);

        await _notifications.CreateInAppAsync(
            userId: cmd.OwnerId,
            title: "New Inquiry Received",
            body: "A tenant has submitted an inquiry on your property.",
            type: NotificationType.NewInquiry,
            relatedEntityType: "inquiry",
            relatedEntityId: inquiry.Id,
            ct: ct
        );

        return MapToResponse(inquiry);
    }

    public static InquiryResponse MapToResponse(Domain.Entities.Inquiry i) => new(
        i.Id, i.PropertyId, i.TenantId, i.OwnerId, i.CompanyId,
        i.Message, i.Status.ToString(), i.AcceptedById,
        i.AcceptedAt, i.ClosedAt, i.CreatedAt);
}