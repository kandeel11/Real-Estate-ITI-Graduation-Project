using AI.Application.DTOs;
using MediatR;

namespace AI.Application.Features.PropertyDescription.Commands.GeneratePropertyDescription
{
    public record GeneratePropertyDescriptionCommand(Guid PropertyId,
    Guid RequestedById,
    string PropertyType,
    string Location,
    int Bedrooms,
    decimal Area,
    List<string> Amenities
) : IRequest<GeneratedDescriptionResponse>;
}
