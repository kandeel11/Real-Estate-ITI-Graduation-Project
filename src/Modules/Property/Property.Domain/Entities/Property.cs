using Domain.Entities;
using Property.Domain.Enums;
using Property.Domain.Events;

namespace Property.Domain.Entities;

public class Property : BaseEntity
{
    public Guid OwnerId { get; private set; }
    public Guid? CompanyId { get; private set; }
    public Guid? ListingBrokerId { get; private set; }

    public string Title { get; private set; } = string.Empty;
    public string TitleAr { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string DescriptionAr { get; private set; } = string.Empty;
    public string? AiGeneratedDescriptionEn { get; private set; }
    public string? AiGeneratedDescriptionAr { get; private set; }

    public PropertyType PropertyType { get; private set; }
    public decimal Price { get; private set; }
    public int Bedrooms { get; private set; }
    public int Bathrooms { get; private set; }
    public decimal AreaSqm { get; private set; }
    public int FloorNumber { get; private set; }
    public int TotalFloors { get; private set; }
    public int YearBuilt { get; private set; }
    public bool IsFurnished { get; private set; }
    public bool PetsAllowed { get; private set; }

    public decimal Latitude { get; private set; }
    public decimal Longitude { get; private set; }
    public string Address { get; private set; } = string.Empty;
    public int? ApartmentNumber { get; private set; }
    public string City { get; private set; } = string.Empty;
    public string District { get; private set; } = string.Empty;

    public ListingSource ListingSource { get; private set; }
    public PropertyStatus Status { get; private set; }

    public bool IsFeatured { get; private set; }
    public DateTime? FeaturedUntil { get; private set; }

    public decimal? AiDuplicateConfidence { get; private set; }
    public VerificationStatus VerificationStatus { get; private set; }
    public string? RejectionReason { get; private set; }

    public int ViewsCount { get; private set; }
    public int InquiriesCount { get; private set; }
    public ICollection<PropertyMedia> Media { get; private set; } = new List<PropertyMedia>();
    private Property() { }



    public static Property Create(
        Guid ownerId,
        Guid? companyId,
        Guid? listingBrokerId,
        string title,
        string titleAr,
        string description,
        string descriptionAr,
        PropertyType propertyType,
        decimal price,
        int bedrooms,
        int bathrooms,
        decimal areaSqm,
        int floorNumber,
        int totalFloors,
        int yearBuilt,
        bool isFurnished,
        bool petsAllowed,
        decimal latitude,
        decimal longitude,
        string address,
        int? apartmentNumber,
        string city,
        string district,
        ListingSource listingSource)
    {
        var property = new Property
        {
            OwnerId = ownerId,
            CompanyId = companyId,
            ListingBrokerId = listingBrokerId,
            Title = title,
            TitleAr = titleAr,
            Description = description,
            DescriptionAr = descriptionAr,
            PropertyType = propertyType,
            Price = price,
            Bedrooms = bedrooms,
            Bathrooms = bathrooms,
            AreaSqm = areaSqm,
            FloorNumber = floorNumber,
            TotalFloors = totalFloors,
            YearBuilt = yearBuilt,
            IsFurnished = isFurnished,
            PetsAllowed = petsAllowed,
            Latitude = latitude,
            Longitude = longitude,
            Address = address,
            ApartmentNumber = apartmentNumber,
            City = city,
            District = district,
            ListingSource = listingSource,
            Status = PropertyStatus.Draft,
            VerificationStatus = VerificationStatus.Pending
        };
        return property;
    }

    public void Update(
        string title,
        string titleAr,
        string description,
        string descriptionAr,
        PropertyType propertyType,
        decimal price,
        int bedrooms,
        int bathrooms,
        decimal areaSqm,
        int floorNumber,
        int totalFloors,
        int yearBuilt,
        bool isFurnished,
        bool petsAllowed,
        decimal latitude,
        decimal longitude,
        string address,
        int? apartmentNumber,
        string city,
        string district,
        Guid? listingBrokerId)
    {
        bool sensitiveFieldChanged = Price != price || Description != description;

        Title = title;
        TitleAr = titleAr;
        Description = description;
        DescriptionAr = descriptionAr;
        PropertyType = propertyType;
        Price = price;
        Bedrooms = bedrooms;
        Bathrooms = bathrooms;
        AreaSqm = areaSqm;
        FloorNumber = floorNumber;
        TotalFloors = totalFloors;
        YearBuilt = yearBuilt;
        IsFurnished = isFurnished;
        PetsAllowed = petsAllowed;
        Latitude = latitude;
        Longitude = longitude;
        Address = address;
        ApartmentNumber = apartmentNumber;
        City = city;
        District = district;
        ListingBrokerId = listingBrokerId;
        UpdatedAt = DateTime.UtcNow;

        if (sensitiveFieldChanged && Status == PropertyStatus.Active)
            VerificationStatus = VerificationStatus.Pending;
    }

    public void SubmitForReview()
    {
        if (Status != PropertyStatus.Draft)
            throw new InvalidOperationException("Only draft properties can be submitted for review.");

        Status = PropertyStatus.PendingReview;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new PropertySubmittedForReviewEvent(Id, OwnerId));
    }

    public void SetVerificationStatus(VerificationStatus verificationStatus, string? rejectionReason = null)
    {
        VerificationStatus = verificationStatus;
        RejectionReason = rejectionReason;
        UpdatedAt = DateTime.UtcNow;

        if (verificationStatus == VerificationStatus.AiVerified || verificationStatus == VerificationStatus.ManuallyVerified)
        {
            Status = PropertyStatus.Active;
            AddDomainEvent(new PropertyActivatedEvent(Id, OwnerId));
        }
        else if (verificationStatus == VerificationStatus.Rejected)
        {
            Status = PropertyStatus.Rejected;
        }
    }

    public void SetFeatured(bool isFeatured, DateTime? featuredUntil)
    {
        IsFeatured = isFeatured;
        FeaturedUntil = featuredUntil;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAiDuplicateConfidence(decimal confidence)
    {
        AiDuplicateConfidence = confidence;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAiDescription(string? descriptionEn, string? descriptionAr)
    {
        AiGeneratedDescriptionEn = descriptionEn;
        AiGeneratedDescriptionAr = descriptionAr;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new PropertyAiDescriptionSetEvent(Id, OwnerId, CompanyId));
    }

    public void AssignBroker(Guid? brokerId)
    {
        ListingBrokerId = brokerId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsRented()
    {
        if (Status != PropertyStatus.Active)
            throw new InvalidOperationException("Only active properties can be marked as rented.");

        Status = PropertyStatus.Rented;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new PropertyRentedEvent(Id, OwnerId, CompanyId));
    }

    public void MarkAsInactive()
    {
        Status = PropertyStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementViews() => ViewsCount++;

    public void SoftDelete()
    {
        DeletedAt = DateTime.UtcNow;
    }
}
