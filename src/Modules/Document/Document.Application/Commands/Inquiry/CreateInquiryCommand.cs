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
    string Message,
    Guid? ListingBrokerId = null
) : IRequest<InquiryResponse>;

public class CreateInquiryCommandHandler : IRequestHandler<CreateInquiryCommand, InquiryResponse>
{
    private readonly IInquiryRepository _inquiries;
    private readonly INotificationService _notifications;
    private readonly ISubscriptionLimitChecker _limitChecker;
    private readonly IDocumentDbContext _db;

    public CreateInquiryCommandHandler(
        IInquiryRepository inquiries,
        INotificationService notifications,
        ISubscriptionLimitChecker limitChecker,
        IDocumentDbContext db)
    {
        _inquiries = inquiries;
        _notifications = notifications;
        _limitChecker = limitChecker;
        _db = db;
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

        // If the property has a listing broker, route the inquiry to them so it
        // shows in their dashboard AND so closed-won leads credit their deals count.
        // Always create the assignment when there's a listing broker — even when the
        // broker is also the owner (independent / company brokers list their own
        // properties), otherwise their won leads would never be counted.
        if (cmd.ListingBrokerId is Guid brokerId)
        {
            _db.InquiryAssignments.Add(new Domain.Entities.InquiryAssignment
            {
                Id = Guid.NewGuid(),
                InquiryId = inquiry.Id,
                AssignedToId = brokerId,
                AssignedById = cmd.OwnerId,
                IsAccepted = false,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(ct);

            // Only send the separate broker notification when the broker isn't already
            // the owner we notified above (avoids a duplicate notification).
            if (brokerId != cmd.OwnerId)
            {
                await _notifications.CreateInAppAsync(
                    userId: brokerId,
                    title: "New Inquiry Received",
                    body: "A tenant has submitted an inquiry on one of your listings.",
                    type: NotificationType.NewInquiry,
                    relatedEntityType: "inquiry",
                    relatedEntityId: inquiry.Id,
                    ct: ct
                );
            }
        }

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