using Document.Application.DTOs;
using Document.Application.Interfaces;
using Document.Domain.Enums;
using Document.Domain.Events;
using MediatR;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;

namespace Document.Application.Commands.Inquiry;

public record AcceptInquiryCommand(Guid InquiryId, Guid AcceptedById) : IRequest<InquiryResponse>;

public class AcceptInquiryCommandHandler : IRequestHandler<AcceptInquiryCommand, InquiryResponse>
{
    private readonly IInquiryRepository _inquiries;
    private readonly IDocumentDbContext _db;
    private readonly INotificationService _notifications;
    private readonly IMediator _mediator;

    public AcceptInquiryCommandHandler(
        IInquiryRepository inquiries,
        IDocumentDbContext db,
        INotificationService notifications,
        IMediator mediator)
    {
        _inquiries = inquiries;
        _db = db;
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

        // عمل Lead أوتوماتيك
        var lead = new Document.Domain.Entities.Lead
        {
            Id = Guid.NewGuid(),
            InquiryId = inquiry.Id,
            PropertyId = inquiry.PropertyId,
            TenantId = inquiry.TenantId,
            OwnerId = inquiry.OwnerId,
            Status = LeadStatus.New,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Leads.Add(lead);

        // عمل Conversation أوتوماتيك
        var conversation = new Document.Domain.Entities.Conversation
        {
            Id = Guid.NewGuid(),
            InquiryId = inquiry.Id,
            PropertyId = inquiry.PropertyId,
            TenantId = inquiry.TenantId,
            BrokerOrOwnerId = cmd.AcceptedById,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Conversations.Add(conversation);

        await _db.SaveChangesAsync(ct);

        var response = CreateInquiryCommandHandler.MapToResponse(inquiry)
            with
        {
            LeadId = lead.Id
        };

        // إرسال notification للـ tenant
        await _notifications.CreateInAppAsync(
            userId: inquiry.TenantId,
            title: "Inquiry Accepted",
            body: "Your inquiry has been accepted. You can now start a conversation.",
            type: NotificationType.InquiryAccepted,
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

        return response;
    }
}