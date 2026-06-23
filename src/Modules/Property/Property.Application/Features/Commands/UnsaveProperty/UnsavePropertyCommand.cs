using MediatR;

namespace Property.Application.Features.Commands.UnsaveProperty;

/// <summary>Removes a property from the tenant's wishlist. Idempotent.</summary>
public record UnsavePropertyCommand(Guid UserId, Guid PropertyId) : IRequest;
