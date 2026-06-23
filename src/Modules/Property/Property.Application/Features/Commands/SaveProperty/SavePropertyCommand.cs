using MediatR;

namespace Property.Application.Features.Commands.SaveProperty;

/// <summary>Adds a property to the tenant's wishlist. Idempotent — saving an
/// already-saved listing is a no-op.</summary>
public record SavePropertyCommand(Guid UserId, Guid PropertyId) : IRequest;
