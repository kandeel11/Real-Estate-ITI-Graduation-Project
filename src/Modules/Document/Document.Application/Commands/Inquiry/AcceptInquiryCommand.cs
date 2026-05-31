using Document.Application.Commands.Inquiry;
using Document.Application.DTOs;
using Document.Application.Interfaces;
using Document.Domain.Enums;
using Document.Domain.Events;
using MediatR;
using RealEstate.Shared.Kernel.Interfaces;

namespace Document.Application.Commands.Inquiry;

public record AcceptInquiryCommand(Guid InquiryId, Guid AcceptedById) : IRequest<InquiryResponse>;

public class AcceptInquiryCommandHandler : IRequestHandler<AcceptInquiryCommand, InquiryResponse>
{
    private readonly IInquiryRepository _inquiries;
    private readonly INotificationService _notifications;
    private readonly IMediator _mediator;

    public AcceptInquiryCommandHandler(
        IInquiryRepository inquiries,
        INotificationService notifications,
        IMediator mediator)
    {
        _inquiries = inquiries;
        _notifications = notifications;
        _mediator = mediator;
    }

    public async Task<InquiryResponse> Handle(AcceptInquiryCommand cmd, CancellationToken ct)
    {
        var inquiry = await _inquiries.GetByIdAsync(cmd.InquiryId, ct)
            ?? throw new KeyNotFoundException($"Inquiry {cmd.InquiryId} not found.");

        if (inquiry.Status != InquiryStatus.Pending)
            throw new InvalidOperationException($"Cannot accept inquiry with status '{inquiry.Status}'.");

        inquiry.Status = InquiryStatus.Accepted;
        inquiry.AcceptedById = cmd.AcceptedById;
        inquiry.AcceptedAt = DateTime.UtcNow;
        inquiry.UpdatedAt = DateTime.UtcNow;

        await _inquiries.UpdateAsync(inquiry, ct);

        await _notifications.CreateInAppAsync(
            userId: inquiry.TenantId,
            title: "Inquiry Accepted",
            body: "Your inquiry has been accepted. You can now start a conversation.",
            type: NotificationType.NewInquiry,
            relatedEntityType: "inquiry",
            relatedEntityId: inquiry.Id,
            ct: ct
        );

        await _mediator.Publish(new InquiryAcceptedEvent
        {
            InquiryId = inquiry.Id,
            PropertyId = inquiry.PropertyId,
            TenantId = inquiry.TenantId,
            OwnerId = inquiry.OwnerId,
            AcceptedById = cmd.AcceptedById
        }, ct);

        return CreateInquiryCommandHandler.MapToResponse(inquiry);
    }
}