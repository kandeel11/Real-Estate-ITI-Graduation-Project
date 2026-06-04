using Domain.Entities;

namespace AI.Domain.Entities
{
    public class SavedSearch:BaseEntity
    {
        public Guid UserId { get; set; } 
        public string Name { get; set; } = string.Empty; 
        public string Filters { get; set; } = string.Empty; // JSON format
        public bool NotifyOnMatch { get; set; } = false; 
        public DateTime? LastMatchedAt { get; set; } 
    }

}
