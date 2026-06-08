using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maintenance.Application.DTOs.Responses
{
    public record AttachmentDto(Guid Id, string FileUrl, string FileType, int FileSizeKb, DateTime UploadedAt);
}
