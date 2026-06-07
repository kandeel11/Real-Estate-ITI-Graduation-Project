using MediatR;

namespace Identity.Domain.Events;

public record CompanyRegisteredEvent(Guid CompanyId, Guid AdminUserId) : INotification;
