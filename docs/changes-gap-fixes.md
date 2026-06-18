# Gap Fixes — Before & After

All changes implemented to close gaps found during the project audit.

---

## Critical Fixes

---

### C1 — AzureBlobMediaService: NullReferenceException on GetSecureUrlAsync

**File:** `src/Modules/Property/Property.Infrastructure/Services/AzureBlobMediaService.cs`

**Problem:** `GetSecureUrlAsync` called `sasBuilder.ToSasQueryParameters(_credential)` but `_credential` was never set — it was `null`. Any call to generate a secure/temporary URL would throw a `NullReferenceException` at runtime.

**Before:**
```csharp
public AzureBlobMediaService(BlobServiceClient blobServiceClient,
    ILogger<AzureBlobMediaService> logger, IOptions<AzureBlobOptions> options)
{
    _blobServiceClient = blobServiceClient;
    _logger = logger;
    _options = options.Value;
    // _credential was never initialized
    _container = _blobServiceClient.GetBlobContainerClient("property-documents");
}
```

**After:**
```csharp
public AzureBlobMediaService(BlobServiceClient blobServiceClient,
    ILogger<AzureBlobMediaService> logger, IOptions<AzureBlobOptions> options)
{
    _blobServiceClient = blobServiceClient;
    _logger = logger;
    _options = options.Value;
    _credential = new StorageSharedKeyCredential(_options.AccountName, _options.AccountKey);
    _container = _blobServiceClient.GetBlobContainerClient("property-documents");
}
```

**Also fixed — `DeleteAsync` blob path extraction:**

Before: `blobName = uri.Segments.Last()` — returned only the last path segment, dropping any nested folder path.

After:
```csharp
var prefix = $"/{containerName}/";
var idx = uri.AbsolutePath.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
blobName = idx >= 0
    ? Uri.UnescapeDataString(uri.AbsolutePath[(idx + prefix.Length)..])
    : Uri.UnescapeDataString(uri.Segments.Last());
```

---

### C2 — Payment Module: Replaced Mock Services with Real Adapters

**Files created:**
- `src/Host/RealEstate.Host/Adapters/DocumentLeaseServiceAdapter.cs`
- `src/Host/RealEstate.Host/Adapters/SmtpNotificationServiceAdapter.cs`

**Problem:** `Payment.Infrastructure` registered `MockLeaseService` and `MockNotificationService` via `TryAddScoped`. Both always returned fake/empty data — no real lease data was read; no real payment notification emails were sent.

**Before:** `AddPaymentModule()` internally called:
```csharp
services.TryAddScoped<ILeaseService, MockLeaseService>();
services.TryAddScoped<INotificationService, MockNotificationService>();
```
`MockLeaseService` returned an empty list. `MockNotificationService` did nothing.

**After:** Real adapters are registered in `Program.cs` **before** `AddPaymentModule()`:
```csharp
builder.Services.AddScoped<ILeaseService, DocumentLeaseServiceAdapter>();
builder.Services.AddScoped<INotificationService, SmtpNotificationServiceAdapter>();
builder.Services.AddPaymentModule(builder.Configuration);
```

Because `TryAddScoped` is a no-op when the type is already registered, the mocks are never used.

**`DocumentLeaseServiceAdapter`** queries `DocumentDbContext.Leases` (active, not deleted, with `RentAmount` and `PaymentDayOfMonth` set), batch-fetches tenant users from `IdentityDbContext`, and maps to `ActiveLeaseDto`.

**`SmtpNotificationServiceAdapter`** delegates `SendEmailAsync` to `IEmailSender` (the existing SMTP sender used by the Identity module). `SendSmsAsync` logs a warning since no SMS provider is configured.

---

### C3 — Subscription Route Collision: Duplicate `api/subscriptions`

**Files:**
- `src/Host/RealEstate.Host/Controllers/SubscriptionController.cs` (wiped to comment stub)
- `src/Host/RealEstate.Host/Controllers/SubscriptionsController.cs` (merged)

**Problem:** Two controllers both declared `[Route("api/subscriptions")]`:
- `SubscriptionController` — `POST api/subscriptions/select-plan`
- `SubscriptionsController` — `GET api/subscriptions/mine`, etc.

While ASP.NET Core could disambiguate by full action path, two controllers sharing the same route prefix is structurally incorrect and causes Swagger conflicts.

**Before:** `SubscriptionController.cs` was a full standalone controller with its own `[Route("api/subscriptions")]`.

**After:** `SubscriptionController.cs` wiped to:
```csharp
// SelectPlan moved to SubscriptionsController (POST api/subscriptions/select-plan)
// to resolve the duplicate [Route("api/subscriptions")] conflict.
```

`SubscriptionsController.cs` gained the merged action:
```csharp
[HttpPost("select-plan")]
[AllowAnonymous]
public async Task<ActionResult<SelectPlanResult>> SelectPlan(
    [FromBody] SelectPlanRequestBody body, CancellationToken ct)
{
    var result = await mediator.Send(
        new SelectPlanCommand(body.UserId, body.CompanyId, body.PlanId, body.BillingCycle), ct);
    return Ok(result);
}

public sealed record SelectPlanRequestBody(Guid UserId, Guid? CompanyId, Guid PlanId, BillingCycle BillingCycle);
```

---

### C4 — `identity.maintenance_companies` Table Always Empty

**File:** `src/Modules/Identity/Identity.Application/Commands/RegisterMaintenanceCompany/RegisterMaintenanceCompanyHandler.cs`

**Problem:** Admin registration of a maintenance company user created a `User` row but never wrote to `identity.maintenance_companies`. All downstream queries that list or search maintenance companies returned empty results.

**Before:**
```csharp
public async Task<Guid> Handle(RegisterMaintenanceCompanyCommand request, CancellationToken cancellationToken)
{
    // ... validation ...
    var user = User.Create(...);
    user.VerifyEmail(user.EmailVerificationToken!);
    await _userRepo.AddAsync(user, cancellationToken);
    await _userRepo.SaveChangesAsync(cancellationToken);
    return user.Id;
    // IMaintenanceCompanyRepository was not injected at all.
}
```

**After:**
```csharp
public async Task<Guid> Handle(RegisterMaintenanceCompanyCommand request, CancellationToken cancellationToken)
{
    // ... validation ...
    var user = User.Create(...);
    user.VerifyEmail(user.EmailVerificationToken!);
    await _userRepo.AddAsync(user, cancellationToken);

    var identityRecord = MaintenanceCompany.Create(
        userId: user.Id,
        companyName: request.FullName,
        serviceTypes: Array.Empty<string>(),
        email: request.Email,
        phone: request.Phone,
        city: string.Empty);

    await _companyRepo.AddAsync(identityRecord, cancellationToken);
    // Both repos share the same IdentityDbContext scope — one SaveChanges saves both.
    await _userRepo.SaveChangesAsync(cancellationToken);

    return user.Id;
}
```

`IMaintenanceCompanyRepository` was injected as a new constructor parameter. `serviceTypes` and `city` default to empty — the MaintenanceCompanies module fills them in when the company completes their profile.

---

## Medium Fixes

---

### M1 — Wallet: Admin Withdrawal Management (Approve / Reject / List All)

**Problem:** `WithdrawalRequest` domain entity had no state-transition methods. `IWithdrawalRepository` had no fetch or update methods. Admin had no way to review or act on pending withdrawals. Owners had no endpoint to see their own withdrawal history.

**Files modified:**

#### `Wallet.Domain/Entities/WithdrawalRequest.cs` — added domain methods

**Before:** entity had only `Create(...)` static factory. No state-transition methods existed.

**After:** added four methods:
```csharp
public void MarkProcessing()                 // Pending → Processing
public void MarkCompleted()                  // Processing → Completed, sets ProcessedAt
public void Approve()                        // Pending → Processing → Completed (combined)
public void Reject(string? notes = null)     // any non-Completed → Rejected, sets ProcessedAt + Notes
```

#### `Wallet.Application/Interfaces/IWithdrawalRepository.cs` — added missing methods

**Before:**
```csharp
Task<Guid> CreateAsync(WithdrawalRequest request, CancellationToken ct = default);
Task<List<WithdrawalRequest>> GetByOwnerIdAsync(Guid ownerId, CancellationToken ct = default);
```

**After:** added:
```csharp
Task<List<WithdrawalRequest>> GetAllAsync(CancellationToken ct = default);
Task<WithdrawalRequest?> GetByIdAsync(Guid id, CancellationToken ct = default);
Task UpdateAsync(WithdrawalRequest request, CancellationToken ct = default);
```

#### `Wallet.Infrastructure/Repositories/WithdrawalRepository.cs` — implemented new methods

```csharp
public Task<List<WithdrawalRequest>> GetAllAsync(CancellationToken ct) =>
    _db.WithdrawalRequests.OrderByDescending(w => w.RequestedAt).ToListAsync(ct);

public Task<WithdrawalRequest?> GetByIdAsync(Guid id, CancellationToken ct) =>
    _db.WithdrawalRequests.FirstOrDefaultAsync(w => w.Id == id, ct);

public async Task UpdateAsync(WithdrawalRequest request, CancellationToken ct)
{
    _db.WithdrawalRequests.Update(request);
    await _db.SaveChangesAsync(ct);
}
```

#### New CQRS files created

| File | Purpose |
|------|---------|
| `Wallet.Application/Commands/ApproveWithdrawal/ApproveWithdrawalCommand.cs` | `record ApproveWithdrawalCommand(Guid WithdrawalId)` |
| `Wallet.Application/Commands/ApproveWithdrawal/ApproveWithdrawalHandler.cs` | Fetches, calls `withdrawal.Approve()`, saves |
| `Wallet.Application/Commands/RejectWithdrawal/RejectWithdrawalCommand.cs` | `record RejectWithdrawalCommand(Guid WithdrawalId, string? Notes)` |
| `Wallet.Application/Commands/RejectWithdrawal/RejectWithdrawalHandler.cs` | Fetches, calls `withdrawal.Reject(notes)`, saves |
| `Wallet.Application/Queries/GetAllWithdrawals/GetAllWithdrawalsQuery.cs` | Admin list — all withdrawals |
| `Wallet.Application/Queries/GetAllWithdrawals/GetAllWithdrawalsHandler.cs` | Returns `GetAllAsync()` |
| `Wallet.Application/Queries/GetOwnerWithdrawals/GetOwnerWithdrawalsQuery.cs` | Owner's own withdrawals |
| `Wallet.Application/Queries/GetOwnerWithdrawals/GetOwnerWithdrawalsHandler.cs` | Returns `GetByOwnerIdAsync(ownerId)` |

#### `WalletController.cs` — added 4 new endpoints

**Before:** 3 endpoints: `GET /balance`, `GET /ledger`, `POST /withdrawals`

**After:** added:
```
GET  /api/wallet/withdrawals                          owner's own withdrawal history
GET  /api/wallet/admin/withdrawals                    admin: all withdrawals (admin role required)
PUT  /api/wallet/admin/withdrawals/{id}/approve       approve (admin role required)
PUT  /api/wallet/admin/withdrawals/{id}/reject        reject with optional notes (admin role required)
```

---

### M2 — Subscription: Missing Event Handlers for Cancel and Upgrade Events

**Problem:** `SubscriptionCancelledEvent` and `SubscriptionUpgradedEvent` were raised by their respective command handlers but had no `INotificationHandler` registered. MediatR silently drops unhandled notifications — events were lost with no trace.

**Files created:**

#### `Subscription.Application/EventHandlers/SubscriptionCancelledEventHandler.cs`

**Before:** No handler existed. Cancellation events were silently discarded.

**After:**
```csharp
public sealed class SubscriptionCancelledEventHandler
    : INotificationHandler<SubscriptionCancelledEvent>
{
    public Task Handle(SubscriptionCancelledEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Subscription cancelled | SubscriptionId: {SubscriptionId} | Reason: {Reason} | At: {CancelledAt}",
            notification.SubscriptionId, notification.Reason, notification.CancelledAt);
        return Task.CompletedTask;
    }
}
```

#### `Subscription.Application/EventHandlers/SubscriptionUpgradedEventHandler.cs`

**Before:** No handler existed. Upgrade events were silently discarded.

**After:**
```csharp
public sealed class SubscriptionUpgradedEventHandler
    : INotificationHandler<SubscriptionUpgradedEvent>
{
    public Task Handle(SubscriptionUpgradedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Subscription upgraded | SubscriptionId: {SubscriptionId} | From: {FromPlanId} → To: {ToPlanId} | At: {UpgradedAt}",
            notification.SubscriptionId, notification.FromPlanId, notification.ToPlanId, notification.UpgradedAt);
        return Task.CompletedTask;
    }
}
```

---

### M3 — Property: Admin Not Notified on Property Submission

**Files:**
- `src/Modules/Property/Property.Application/Interfaces/IAdminEmailService.cs` (created)
- `src/Modules/Property/Property.Application/Features/EventHandlers/PropertySubmittedAdminNotificationHandler.cs` (modified)
- `src/Host/RealEstate.Host/Adapters/PropertyAdminEmailAdapter.cs` (created)
- `src/Host/RealEstate.Host/appsettings.json` (added `AdminEmail` key)

**Problem:** When a property was submitted for review, the event handler only called `LogWarning` — no email was ever sent to the admin.

**Before:**
```csharp
public Task Handle(PropertySubmittedForReviewEvent notification, CancellationToken cancellationToken)
{
    _logger.LogWarning("TODO: notify admin about property {PropertyId}", notification.PropertyId);
    return Task.CompletedTask;
}
```

**After:** New `IAdminEmailService` interface:
```csharp
public interface IAdminEmailService
{
    Task NotifyPropertySubmittedAsync(
        Guid propertyId, string title, Guid ownerId, string city, CancellationToken ct = default);
}
```

Handler now calls the service (errors are swallowed so a failed email never breaks event dispatch):
```csharp
public async Task Handle(PropertySubmittedForReviewEvent notification, CancellationToken cancellationToken)
{
    _logger.LogInformation("Property submitted for review | PropertyId: {PropertyId}...", ...);
    try
    {
        await _adminEmail.NotifyPropertySubmittedAsync(
            notification.PropertyId, notification.Title,
            notification.OwnerId, notification.City, cancellationToken);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to send admin email for property {PropertyId}", notification.PropertyId);
    }
}
```

`PropertyAdminEmailAdapter` reads `config["AdminEmail"]` and sends via `IEmailSender`. Registered in `Program.cs`:
```csharp
builder.Services.AddScoped<Property.Application.Interfaces.IAdminEmailService, PropertyAdminEmailAdapter>();
```

`appsettings.json` now includes `"AdminEmail": "kandeelmohamed241@gmail.com"`.

---

### M4 — Leads: Admin Has No Endpoint to List All Leads

**Files:**
- `src/Modules/Document/Document.Application/Queries/Lead/GetAllLeads/GetAllLeadsQuery.cs` (created)
- `src/Modules/Document/Document.Application/Queries/Lead/GetAllLeads/GetAllLeadsHandler.cs` (created)
- `src/Host/RealEstate.Host/Controllers/LeadsController.cs` (modified)

**Problem:** `LeadsController` only had `GET /api/leads/broker/{brokerId}` for brokers, and no admin-wide list endpoint. Admins had no way to view all leads across the platform.

**Before:** `LeadsController.cs` had `PUT /{id}/status` and `GET /broker/{brokerId}` only.

**After:** New query:
```csharp
public record GetAllLeadsQuery(int Page = 1, int PageSize = 20, string? Status = null)
    : IRequest<List<LeadDto>>;
```

Handler filters by `LeadStatus` enum (parsed case-insensitively), orders by `UpdatedAt` descending, applies page/size pagination.

New endpoint in `LeadsController`:
```csharp
[HttpGet]
[Authorize(Roles = "admin")]
public async Task<IActionResult> GetAll(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    [FromQuery] string? status = null,
    CancellationToken ct = default)
{
    var leads = await _mediator.Send(new GetAllLeadsQuery(page, pageSize, status), ct);
    return Ok(leads);
}
```

Route: `GET /api/leads?page=1&pageSize=20&status=Active`

---

## Low-Priority Fixes

---

### L1 — SubscriptionPlansController: Commented-Out Duplicate

**File:** `src/Host/RealEstate.Host/Controllers/SubscriptionPlansController.cs`

**Problem:** File contained a large block of fully commented-out C# — a dead duplicate of functional subscription plan endpoints.

**Before:** Large commented-out controller body.

**After:** Wiped to a single comment explaining where the real implementation lives.

---

### L2 — MockPaymentGatewayService: Dead File

**File:** `src/Modules/Payment/Payment.Infrastructure/Services/MockPaymentGatewayService.cs`

**Problem:** File still contained a full mock implementation (always returning `IsSuccess = true` with a fake transaction ID). The real `StripePaymentGatewayService` was registered in DI — this mock was never used but was misleading.

**Before:**
```csharp
public class MockPaymentGatewayService : IPaymentGatewayService
{
    public Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, ...)
        => Task.FromResult(new PaymentResult
        {
            IsSuccess = true,
            TransactionId = $"mock-{Guid.NewGuid()}"
        });
}
```

**After:** Wiped to comment stub:
```csharp
// Replaced by StripePaymentGatewayService — this mock is no longer registered.
```

---

## Summary Table

| # | Category | File(s) Changed | What Changed |
|---|----------|----------------|--------------|
| C1 | Critical | `AzureBlobMediaService.cs` | Initialized `_credential`; fixed `DeleteAsync` path extraction for nested blob paths |
| C2 | Critical | `DocumentLeaseServiceAdapter.cs`, `SmtpNotificationServiceAdapter.cs`, `Program.cs` | Real lease + notification adapters replace mocks in Payment module |
| C3 | Critical | `SubscriptionController.cs`, `SubscriptionsController.cs` | Merged SelectPlan into one controller; removed duplicate route |
| C4 | Critical | `RegisterMaintenanceCompanyHandler.cs` | Inserts into `identity.maintenance_companies` on every registration |
| M1 | Medium | `WithdrawalRequest.cs`, `IWithdrawalRepository.cs`, `WithdrawalRepository.cs`, 6 new CQRS files, `WalletController.cs` | Full admin approve/reject flow + owner withdrawal history |
| M2 | Medium | `SubscriptionCancelledEventHandler.cs`, `SubscriptionUpgradedEventHandler.cs` | Subscribe lifecycle events now logged (were silently dropped) |
| M3 | Medium | `IAdminEmailService.cs`, `PropertySubmittedAdminNotificationHandler.cs`, `PropertyAdminEmailAdapter.cs`, `appsettings.json` | Admin receives email when property is submitted for review |
| M4 | Medium | `GetAllLeadsQuery.cs`, `GetAllLeadsHandler.cs`, `LeadsController.cs` | Admin `GET /api/leads` with pagination and optional status filter |
| L1 | Low | `SubscriptionPlansController.cs` | Removed commented-out duplicate code |
| L2 | Low | `MockPaymentGatewayService.cs` | Removed dead mock |
