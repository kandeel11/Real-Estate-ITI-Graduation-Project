using Document.Application.DTOs;
using Document.Application.Interfaces;
using Document.Domain.Enums;
using MediatR;

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

    public CreateInquiryCommandHandler(
        IInquiryRepository inquiries,
        INotificationService notifications)
    {
        _inquiries = inquiries;
        _notifications = notifications;
    }

    public async Task<InquiryResponse> Handle(CreateInquiryCommand cmd, CancellationToken ct)
    {
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

    public static InquiryResponse MapToResponse(Domain.Entities.Inquiry i, Guid? leadId = null) => new(
        i.Id, i.PropertyId, i.TenantId, i.OwnerId, i.CompanyId,
        i.Message, i.Status.ToString(), i.AcceptedById,
        i.AcceptedAt, i.ClosedAt, i.CreatedAt)
    {
        LeadId = leadId
    };
}