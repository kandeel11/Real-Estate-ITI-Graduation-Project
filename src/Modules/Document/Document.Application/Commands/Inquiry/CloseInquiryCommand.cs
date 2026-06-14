using Document.Application.Interfaces;
using Document.Domain.Enums;
using MediatR;

namespace Document.Application.Commands.Inquiry;

public record CloseInquiryCommand(Guid InquiryId) : IRequest;

public class CloseInquiryCommandHandler : IRequestHandler<CloseInquiryCommand>
{
    private readonly IInquiryRepository _inquiries;

    public CloseInquiryCommandHandler(IInquiryRepository inquiries) => _inquiries = inquiries;

    public async Task Handle(CloseInquiryCommand cmd, CancellationToken ct)
    {
        var inquiry = await _inquiries.GetByIdAsync(cmd.InquiryId, ct)
            ?? throw new KeyNotFoundException($"Inquiry {cmd.InquiryId} not found.");

        inquiry.Status = InquiryStatus.Closed;
        inquiry.ClosedAt = DateTime.UtcNow;
        inquiry.UpdatedAt = DateTime.UtcNow;

        await _inquiries.UpdateAsync(inquiry, ct);
    }
}