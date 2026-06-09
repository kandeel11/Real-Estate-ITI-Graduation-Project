using MediatR;

namespace Document.Application.Commands.Rating.CreateRating;

public record CreateRatingCommand(
    Guid ReviewerId,
    Guid? LeaseId,
    Guid? PropertyId,
    Guid? CompanyId,
    Guid? BrokerId,
    Guid? MaintenanceCompanyId,
    int Rating,
    string? ReviewText) : IRequest<Guid>;