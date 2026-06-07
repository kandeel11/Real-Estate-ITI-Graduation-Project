using MediatR;

namespace RealEstate.Shared.Kernel.Shared.IntegrationEvents;

/// <summary>
/// Fired by the Identity module when a company account is registered.
/// Subscription module handles this to auto-assign the Free Company plan.
/// AdminUserId is the user who created/owns the company account.
/// </summary>
public record CompanyCreatedEvent(Guid CompanyId, Guid AdminUserId) : INotification;
