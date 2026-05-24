using Domain.Entities;
using Property.Domain.Enums;

namespace Property.Domain.Entities;

public class PropertyMedia : BaseEntity
{
    public Guid PropertyId { get; private set; }
    public string MediaUrl { get; private set; } = string.Empty;
    public MediaType MediaType { get; private set; }
    public bool IsPrimary { get; private set; }
    public int SortOrder { get; private set; }
    public int FileSizeKb { get; private set; }
    public DateTime UploadedAt { get; private set; }

    private PropertyMedia() { }

    public static PropertyMedia Create(
        Guid propertyId,
        string mediaUrl,
        MediaType mediaType,
        int fileSizeKb,
        int sortOrder = 0)
    {
        return new PropertyMedia
        {
            PropertyId = propertyId,
            MediaUrl = mediaUrl,
            MediaType = mediaType,
            FileSizeKb = fileSizeKb,
            SortOrder = sortOrder,
            IsPrimary = false,
            UploadedAt = DateTime.UtcNow
        };
    }

    public void SetPrimary(bool isPrimary) => IsPrimary = isPrimary;

    public void UpdateSortOrder(int sortOrder) => SortOrder = sortOrder;
}
