namespace Property.Application.DTOs;

public class PropertyMediaDto
{
    public Guid Id { get; init; }
    public string MediaUrl { get; init; } = string.Empty;
    public string MediaType { get; init; } = string.Empty;
    public bool IsPrimary { get; init; }
    public int SortOrder { get; init; }
    public int FileSizeKb { get; init; }
    public DateTime UploadedAt { get; init; }
}
