namespace AI.Application.DTOs;

// DTO لرد الـ Query الخاصة بالقائمة الجانبية
public class ChatbotSessionSummaryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}

// DTO لرسالة واحدة جوا السيشن
public class ChatbotMessageDto
{
    public Guid Id { get; set; }
    public string Role { get; set; } = string.Empty; // "user" or "assistant"
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// DTO لرد السيشن بالرسايل بتاعتها
public class SessionHistoryDto
{
    public Guid SessionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public List<ChatbotMessageDto> Messages { get; set; } = new();
}