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
    public string Country { get; private set; } = string.Empty;
    public string Governorate { get; private set; } = string.Empty;



    public string Address { get; private set; } = string.Empty;
    public int? ApartmentNumber { get; private set; }
    public string City { get; private set; } = string.Empty;
    public string District { get; private set; } = string.Empty;
    public string OwnerFullName { get; private set; } = string.Empty;
    public string NationalIdBlobPath { get; private set; } = string.Empty;
    public string OwnershipProofBlobPath { get; private set; } = string.Empty;
    public string OwnerShipStatus { get; private set; } = "Pending"; // e.g. "verified", "pending", "rejected"
    public ListingSource ListingSource { get; private set; }
    public PropertyStatus Status { get; private set; }

    public bool IsFeatured { get; private set; }
    public DateTime? FeaturedUntil { get; private set; }

    public decimal? AiDuplicateConfidence { get; private set; }
    public VerificationStatus VerificationStatus { get; private set; }
    public string? RejectionReason { get; private set; }

    // Two independent verification gates. The property only goes Active when BOTH are clear.
    public GateStatus OwnershipGate { get; private set; } = GateStatus.Pending;
    public GateStatus DuplicateGate { get; private set; } = GateStatus.Pending;

    public int ViewsCount { get; private set; }
    public int InquiriesCount { get; private set; }

    // ── Full-management flags (set when a Company Broker manages the property) ─
    public bool IsFullManagement { get; private set; }
    public bool IsMaintenanceEnabled { get; private set; }
    public bool IsPaymentEnabled { get; private set; }

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
        string address,
        int? apartmentNumber,
        string country, string governorate,
        string ownerShipStatus,
        string ownerFullName,
    string nationalIdBlobPath,
    string ownershipProofBlobPath,
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
            Address = address,
            OwnerFullName = ownerFullName,
            NationalIdBlobPath = nationalIdBlobPath,
            OwnershipProofBlobPath = ownershipProofBlobPath,
            OwnerShipStatus = ownerShipStatus,
            ApartmentNumber = apartmentNumber,
            City = city,
            District = district,
            Country = country,
            Governorate = governorate,
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
        string address,
        int? apartmentNumber,
        string city,
        string district,
        Guid? listingBrokerId,
        string? ownerShipStatus)
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
        Address = address;
        ApartmentNumber = apartmentNumber;
        City = city;
        District = district;
        ListingBrokerId = listingBrokerId;
        OwnerShipStatus = ownerShipStatus;
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

    /// <summary>
    /// Admin override: forces a verification status directly (e.g. approve/reject from the
    /// admin endpoint), bypassing the automatic gate evaluation.
    /// </summary>
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

    // ── Two-gate verification ────────────────────────────────────────────────
    // Ownership gate (owners only) and duplicate gate (everyone) are set independently
    // by their respective AI flows. EvaluateGates() derives the lifecycle status from both.

    /// <summary>Ownership doesn't apply (e.g. a broker listing) — the gate auto-passes.</summary>
    public void MarkOwnershipNotRequired()
    {
        OwnershipGate = GateStatus.NotRequired;
        UpdatedAt = DateTime.UtcNow;
        EvaluateGates();
    }

    public void SetOwnershipGate(GateStatus status, string? note = null)
    {
        OwnershipGate = status;
        UpdatedAt = DateTime.UtcNow;
        if (note is not null && status is GateStatus.Failed or GateStatus.NeedsReview)
            RejectionReason = note;
        EvaluateGates();
    }

    public void SetDuplicateGate(GateStatus status, string? note = null)
    {
        DuplicateGate = status;
        UpdatedAt = DateTime.UtcNow;
        if (note is not null && status is GateStatus.Failed or GateStatus.NeedsReview)
            RejectionReason = note;
        EvaluateGates();
    }

    /// <summary>
    /// Derives Status/VerificationStatus from the two gates:
    ///   • either gate Failed            → Rejected
    ///   • both gates clear              → Active (AiVerified)
    ///   • either gate NeedsReview       → RequiresManualReview (status unchanged)
    ///   • otherwise (a gate Pending)    → left as-is
    /// </summary>
    private void EvaluateGates()
    {
        bool ownershipClear = OwnershipGate is GateStatus.Passed or GateStatus.NotRequired;
        bool duplicateClear = DuplicateGate is GateStatus.Passed or GateStatus.NotRequired;

        if (OwnershipGate == GateStatus.Failed || DuplicateGate == GateStatus.Failed)
        {
            VerificationStatus = VerificationStatus.Rejected;
            // Do not hard-reject a Draft property — the owner may still be completing the
            // wizard (photos, description).  The Rejected gate is visible via VerificationStatus
            // and the admin queue; the property transitions to Rejected only once it has
            // left the Draft state (e.g. submitted for admin review).
            if (Status != PropertyStatus.Draft)
                Status = PropertyStatus.Rejected;
        }
        else if (ownershipClear && duplicateClear)
        {
            VerificationStatus = VerificationStatus.AiVerified;
            if (Status != PropertyStatus.Active)
            {
                Status = PropertyStatus.Active;
                AddDomainEvent(new PropertyActivatedEvent(Id, OwnerId));
            }
        }
        else if (OwnershipGate == GateStatus.NeedsReview || DuplicateGate == GateStatus.NeedsReview)
        {
            VerificationStatus = VerificationStatus.RequiresManualReview;
            // Status left unchanged (Draft / PendingReview) — waiting on an admin.
        }
        // else: a gate is still Pending → leave everything as-is.
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
        // Merged model: the accepted AI text becomes the listing's main description.
        if (!string.IsNullOrWhiteSpace(descriptionEn)) Description = descriptionEn;
        if (!string.IsNullOrWhiteSpace(descriptionAr)) DescriptionAr = descriptionAr;
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

    public void MarkAsRentedWithManagement(bool isFullManagement, bool isMaintenanceEnabled, bool isPaymentEnabled)
    {
        MarkAsRented();
        IsFullManagement = isFullManagement;
        IsMaintenanceEnabled = isMaintenanceEnabled;
        IsPaymentEnabled = isPaymentEnabled;
    }

    /// <summary>
    /// Returns a rented property to the market when its lease ends. Idempotent —
    /// a no-op unless the property is currently Rented, so the daily expiry sweep
    /// can run safely without double-restoring the usage slot.
    /// </summary>
    public void MarkAsAvailable()
    {
        if (Status != PropertyStatus.Rented)
            return;

        Status = PropertyStatus.Active;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new PropertyBecameAvailableEvent(Id, OwnerId, CompanyId));
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
