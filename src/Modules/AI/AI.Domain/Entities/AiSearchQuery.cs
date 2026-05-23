using Domain.Entities;

namespace AI.Domain.Entities
{
    public class AiSearchQuery : BaseEntity
    {
        public Guid? UserId { get; set; } // Null if anonymous 
        public string RawQuery { get; set; } = string.Empty; // 
        public string ParsedFilters { get; set; } = string.Empty; // JSON format 
        public int ResultsCount { get; set; }   
        public string? SessionId { get; set; }
        public string? IpAddress { get; set; }
    }
}
