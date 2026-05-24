using MediatR;

namespace Property.Application.Features.Commands.SetAiDescription;

public record SetAiDescriptionCommand(
    Guid PropertyId,
    string? DescriptionEn,
    string? DescriptionAr
) : IRequest;
