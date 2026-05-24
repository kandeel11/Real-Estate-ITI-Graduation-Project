using MediatR;
namespace Domain.Entities
{
    public abstract class BaseEntity
    {
        // Primary Key
        public Guid Id { get; protected set; } = Guid.NewGuid();

        // Audit Fields
        public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; protected set; }
        // Soft Delete Field
        public DateTime? DeletedAt { get; protected set; }

        // Helper property (Optional but useful in queries)
        public bool IsDeleted => DeletedAt.HasValue;
        
        // --- Domain Events Management for CQRS/MediatR ---
        private readonly List<INotification> _domainEvents = new ();
        // Expose events as read-only collection
        public IReadOnlyCollection<INotification> DomainEvents => _domainEvents.AsReadOnly();

        public void AddDomainEvent(INotification domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        public void RemoveDomainEvent(INotification domainEvent)
        {
            _domainEvents.Remove(domainEvent);
        }

        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }

    }
}
