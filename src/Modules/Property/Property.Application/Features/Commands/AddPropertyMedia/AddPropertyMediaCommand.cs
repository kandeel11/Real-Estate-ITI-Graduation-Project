using MediatR;
using Property.Domain.Enums;

namespace Property.Application.Features.Commands.AddPropertyMedia;

public record AddPropertyMediaCommand(
    Guid PropertyId,
    Guid RequestingUserId,
    Stream FileStream,
    string FileName,
    string ContentType,
    MediaType MediaType,
    int FileSizeKb,
    int SortOrder = 0
) : IRequest<Guid>;
