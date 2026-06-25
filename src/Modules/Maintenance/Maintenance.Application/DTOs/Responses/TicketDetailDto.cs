using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maintenance.Application.DTOs.Responses
{
    public record TicketDetailDto(
        Guid Id,
        string TicketNumber,
        Guid PropertyId,
        string? PropertyAddress,
        Guid TenantId,
        string? TenantEmail,
        string? TenantPhone,
        Guid OwnerId,
        string Title,
        string Description,
        string Priority,
        string Status,
        string? ResponsibleParty,
        string? ResolutionMethod,
        AiEvaluationDto? AiEvaluation,
        VendorAssignmentDto? VendorAssignment,
        List<AttachmentDto> Attachments,
        List<CommentDto> Comments,
        List<StatusHistoryItemDto> StatusHistory,
        PaymentDto? Payment,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        DateTime? ResolvedAt,
        Guid? RoutedToCompanyId = null,
        Guid? ParentTicketId = null);
}
