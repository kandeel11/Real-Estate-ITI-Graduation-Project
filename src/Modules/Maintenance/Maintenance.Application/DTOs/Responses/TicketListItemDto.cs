using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maintenance.Application.DTOs.Responses
{
    public record TicketListItemDto(
        Guid Id, 
        string TicketNumber, 
        string Title, 
        string Priority, 
        string Status, 
        string? PropertyAddress, 
        int AttachmentCount, 
        DateTime CreatedAt
        );
}
