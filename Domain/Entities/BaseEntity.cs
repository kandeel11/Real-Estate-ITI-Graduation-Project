using MediatR;
namespace Domain.Entities
{
    public abstract class BaseEntity
    {
        // Primary Key
        public Guid Id { get; protected set; } 

        // Audit Fields
        public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; protected set; }
        // Soft Delete Field
        public DateTime? DeletedAt { get; protected set; }

        // Helper property (Optional but useful in queries)
        public bool IsDeleted => DeletedAt.HasValue;
        protected BaseEntity()
        {
            Id = Guid.CreateVersion7(); //  sequential + unique + no clustering issue
        }

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
        // --- End of Domain Events Management ---
        // Methods to manage audit fields
        public void MarkAsUpdated()
        {
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkAsDeleted()
        {
            DeletedAt = DateTime.UtcNow;
        }

    }
}
