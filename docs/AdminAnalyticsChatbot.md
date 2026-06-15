# Admin Data Analytics Chatbot — Implementation Documentation

## Overview

This document describes the architecture and implementation of the **Admin Analytics Chatbot** feature — an AI-powered assistant that allows administrators to ask natural language questions about live system data (properties, payments, subscriptions, users) and receive data-grounded answers.

---

## Architecture Decisions

| Constraint | Solution |
|---|---|
| AI module cannot reference other modules' DbContexts | Four `IXxxAnalyticsService` contracts defined in `RealEstate.Shared.Kernel`. Each module implements its own contract against its own DbContext. The AI module only sees the Shared interfaces. |
| Plugin must live in Application layer, not Infrastructure | `AdminAnalyticsPlugin` is in `AI.Application`. It depends solely on Shared interfaces — no EF, no Infrastructure services. |
| Separate from existing user chatbot | Dedicated endpoint, dedicated MediatR query, dedicated system prompt, dedicated Kernel clone with a different plugin set. |
| No new project references needed | `AI.Application` already referenced `RealEstate.Shared.Kernel` and `Microsoft.SemanticKernel.Core`. The Host already referenced `AI.Infrastructure` transitively. |
| Provider-agnostic Kernel configuration | Used `FunctionChoiceBehavior.Auto()` (SemanticKernel.Core) instead of `OpenAIPromptExecutionSettings.ToolCallBehavior` (OpenAI connector) — keeps Application layer free of provider-specific packages. |

---

## Data Flow

```
Admin (HTTP Client)
    │
    │  POST /api/ai/admin/analytics/chat
    │  Authorization: Bearer <admin-token>
    │  Body: { "prompt": "How many properties were added this month?" }
    │
    ▼
AIController.AdminAnalyticsChat()
    │  [Authorize(Policy = "AdminOnly")]  →  requires role: "admin"
    │
    ▼
ISender.Send(AdminAnalyticsQuery)
    │
    ▼
AdminAnalyticsQueryHandler.Handle()
    │
    ├── Clone scoped Kernel (inherits OpenAI chat completion service)
    ├── Strip all inherited plugins (PropertySearch, ContractRetrieval)
    ├── Add AdminAnalyticsPlugin with name "AdminAnalytics"
    ├── Build ChatHistory with admin system prompt + user question
    ├── Set FunctionChoiceBehavior.Auto()
    │
    ▼
IChatCompletionService.GetChatMessageContentAsync()
    │
    │  LLM decides which tool(s) to call based on the question
    │
    ├──► GetNewPropertyListings(from, to)
    │       └── IPropertyAnalyticsService.GetNewListingsCountAsync()
    │               └── Property.Infrastructure → PropertyDbContext
    │
    ├──► GetPaymentRevenueSummary()
    │       └── IPaymentAnalyticsService.GetSummaryAsync()
    │               └── Payment.Infrastructure → PaymentDbContext
    │
    ├──► GetSubscriptionOverview()
    │       └── ISubscriptionAnalyticsService.GetSummaryAsync()
    │               └── Subscription.Infrastructure → SubscriptionDbContext
    │
    └──► GetUserOverview()
            └── IUserAnalyticsService.GetSummaryAsync()
                    └── Identity.Infrastructure → IdentityDbContext
    │
    ▼
LLM synthesizes tool results into a natural language answer
    │
    ▼
AdminAnalyticsResponse { Answer = "There were 47 new property listings added in June 2025..." }
    │
    ▼
HTTP 200 OK  { "answer": "..." }
```

---

## Files Created / Modified

### Step 1 — Shared Contracts

**Location:** `src/Shared/RealEstate.Shared.Kernel/Analytics/`

```
Analytics/
├── IPropertyAnalyticsService.cs
├── IPaymentAnalyticsService.cs
├── ISubscriptionAnalyticsService.cs
└── IUserAnalyticsService.cs
```

#### `IPropertyAnalyticsService.cs`

```csharp
namespace RealEstate.Shared.Kernel.Analytics;

public interface IPropertyAnalyticsService
{
    Task<PropertyAnalyticsSummaryDto> GetSummaryAsync(CancellationToken ct = default);
    Task<int> GetNewListingsCountAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<CityListingCountDto>> GetTopCitiesAsync(int topN = 5, CancellationToken ct = default);
    Task<IReadOnlyList<PropertyTypeCountDto>> GetCountsByTypeAsync(CancellationToken ct = default);
}

public record PropertyAnalyticsSummaryDto(
    int Total, int Active, int Rented,
    int PendingVerification, int Rejected, int TotalActiveLeases);

public record CityListingCountDto(string City, int Count);
public record PropertyTypeCountDto(string PropertyType, int Count);
```

#### `IPaymentAnalyticsService.cs`

```csharp
namespace RealEstate.Shared.Kernel.Analytics;

public interface IPaymentAnalyticsService
{
    Task<PaymentAnalyticsSummaryDto> GetSummaryAsync(CancellationToken ct = default);
    Task<decimal> GetRevenueInRangeAsync(DateTime from, DateTime to, CancellationToken ct = default);
}

public record PaymentAnalyticsSummaryDto(
    decimal TotalCollected, decimal TotalPending,
    decimal TotalLateFees, int OverdueCount, int PaidCount);
```

#### `ISubscriptionAnalyticsService.cs`

```csharp
namespace RealEstate.Shared.Kernel.Analytics;

public interface ISubscriptionAnalyticsService
{
    Task<SubscriptionAnalyticsSummaryDto> GetSummaryAsync(CancellationToken ct = default);
    Task<int> GetNewSubscriptionsCountAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<int> GetChurnedSubscriptionsCountAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<PlanSubscriptionCountDto>> GetCountsByPlanAsync(CancellationToken ct = default);
}

public record SubscriptionAnalyticsSummaryDto(
    int TotalActive, int TotalTrial,
    int TotalExpiredOrCancelled, decimal TotalRevenueCollected);

public record PlanSubscriptionCountDto(string PlanName, string Tier, int ActiveCount);
```

#### `IUserAnalyticsService.cs`

```csharp
namespace RealEstate.Shared.Kernel.Analytics;

public interface IUserAnalyticsService
{
    Task<UserAnalyticsSummaryDto> GetSummaryAsync(CancellationToken ct = default);
    Task<int> GetNewUsersCountAsync(DateTime from, DateTime to, CancellationToken ct = default);
}

public record UserAnalyticsSummaryDto(int TotalUsers, int TotalCompanies, int TotalBrokers);
```

---

### Step 2 — Semantic Kernel Plugin

**Location:** `src/Modules/AI/AI.Application/Features/AdminAnalytics/Plugins/AdminAnalyticsPlugin.cs`

The plugin is a plain C# class decorated with `[KernelFunction]` methods. Semantic Kernel invokes these automatically when the LLM decides data retrieval is needed.

#### Kernel Functions

| Function Name | Answers Questions Like |
|---|---|
| `GetPropertyOverview` | "How many properties are there?" / "What's the status breakdown?" |
| `GetNewPropertyListings(from, to)` | "How many listings were added this week/today?" |
| `GetTopCitiesByListings(topN)` | "Which cities have the most listings?" |
| `GetPropertyBreakdownByType` | "How many apartments vs villas?" |
| `GetPaymentRevenueSummary` | "What is the total revenue?" / "How many overdue payments?" |
| `GetPaymentRevenueInRange(from, to)` | "What was the revenue in June?" |
| `GetSubscriptionOverview` | "How many active subscriptions?" / "Total subscription revenue?" |
| `GetSubscriptionsByPlan` | "Which plan is most popular?" |
| `GetSubscriptionActivity(from, to)` | "How many new subs this month?" / "Churn rate this quarter?" |
| `GetUserOverview` | "How many users/companies are registered?" |
| `GetNewUsersInRange(from, to)` | "How many users signed up this week?" |

**Design notes:**
- Date parameters are `string` (not `DateTime`) — the LLM passes date strings; parsing them internally allows returning a clean error message to the model on bad input rather than throwing an unhandled exception.
- All methods catch exceptions and return descriptive error strings — the LLM receives these and can relay them to the admin gracefully.
- No DbContext, no repository, no module reference — only the four Shared interfaces.

---

### Step 3 — MediatR Query & Handler

**Location:** `src/Modules/AI/AI.Application/Features/AdminAnalytics/`

```
Features/AdminAnalytics/
├── Plugins/
│   └── AdminAnalyticsPlugin.cs       ← Step 2
├── AdminAnalyticsQuery.cs            ← Query + Response records
└── AdminAnalyticsQueryHandler.cs     ← Handler
```

#### `AdminAnalyticsQuery.cs`

```csharp
public sealed record AdminAnalyticsQuery(string Prompt) : IRequest<AdminAnalyticsResponse>;
public sealed record AdminAnalyticsResponse(string Answer);
```

#### Handler logic (`AdminAnalyticsQueryHandler.cs`)

1. **Clone** the globally scoped `Kernel` — prevents mutating the shared instance used by the user chatbot.
2. **Strip** all inherited plugins (`PropertySearchPlugin`, `ContractRetrievalPlugin`) from the clone.
3. **Install** `AdminAnalyticsPlugin` on the clean clone.
4. Build `ChatHistory` with the **admin system prompt** (English-only, data-mandatory, no property-search scope).
5. Set `FunctionChoiceBehavior.Auto()` — provider-agnostic, keeps Application layer free of the OpenAI connector package.
6. Invoke `IChatCompletionService.GetChatMessageContentAsync()`.
7. Return the LLM's answer as `AdminAnalyticsResponse`.

#### DI registration (`AI.Application/DependencyInjection.cs`)

```csharp
services.AddScoped<AdminAnalyticsPlugin>();
```

---

### Step 4 — HTTP Endpoint

**File modified:** `src/Host/RealEstate.Host/Controllers/AIController.cs`

```
POST /api/ai/admin/analytics/chat
Authorization: Bearer <token>   (role: admin)
Content-Type: application/json

Body:
{
  "prompt": "What is the total rent revenue collected this month?"
}

Response 200 OK:
{
  "answer": "The total rent revenue collected in June 2025 is EGP 2,340,000..."
}

Response 409 Conflict (AI service unavailable):
{
  "error": "The analytics assistant is temporarily unavailable. Please try again later."
}

Response 403 Forbidden (non-admin token)
```

**Security:** Reuses the existing `"AdminOnly"` policy defined in `Program.cs` — `RequireRole("admin")`. No new policy was needed.

---

## What You Need to Implement Next

To make the feature fully operational, implement the four analytics services in their respective modules and register them:

### Property.Infrastructure

```csharp
// Services/PropertyAnalyticsService.cs
public class PropertyAnalyticsService : IPropertyAnalyticsService
{
    private readonly PropertyDbContext _db;
    // implement using _db.Properties, _db.PropertyAnalytics ...
}

// DependencyInjection.cs
services.AddScoped<IPropertyAnalyticsService, PropertyAnalyticsService>();
```

### Payment.Infrastructure

```csharp
// Services/PaymentAnalyticsService.cs
public class PaymentAnalyticsService : IPaymentAnalyticsService
{
    private readonly PaymentDbContext _db;
    // implement using _db.RentPayments ...
}

// DependencyInjection.cs
services.AddScoped<IPaymentAnalyticsService, PaymentAnalyticsService>();
```

### Subscription.Infrastructure

```csharp
// Services/SubscriptionAnalyticsService.cs
public class SubscriptionAnalyticsService : ISubscriptionAnalyticsService
{
    private readonly SubscriptionDbContext _db;
    // implement using _db.Subscriptions, _db.Payments ...
}

// DependencyInjection.cs
services.AddScoped<ISubscriptionAnalyticsService, SubscriptionAnalyticsService>();
```

### Identity.Infrastructure

```csharp
// Services/UserAnalyticsService.cs
public class UserAnalyticsService : IUserAnalyticsService
{
    private readonly IdentityDbContext _db;
    // implement using _db.Users, _db.Companies ...
}

// DependencyInjection.cs
services.AddScoped<IUserAnalyticsService, UserAnalyticsService>();
```

---

## Module Boundary Compliance

```
┌─────────────────────────────────────────────────────────────────┐
│  AI.Application                                                 │
│  AdminAnalyticsPlugin                                           │
│      depends on ↓                                               │
├─────────────────────────────────────────────────────────────────┤
│  RealEstate.Shared.Kernel                                       │
│  IPropertyAnalyticsService  ◄──── Property.Infrastructure       │
│  IPaymentAnalyticsService   ◄──── Payment.Infrastructure        │
│  ISubscriptionAnalyticsService ◄── Subscription.Infrastructure  │
│  IUserAnalyticsService      ◄──── Identity.Infrastructure       │
└─────────────────────────────────────────────────────────────────┘

AI module never references:   PropertyDbContext
                               PaymentDbContext
                               SubscriptionDbContext
                               IdentityDbContext
```

---

## Example Prompts

| Admin Question | Plugin(s) Called |
|---|---|
| "How many properties are there in total?" | `GetPropertyOverview` |
| "How many new listings were added this week?" | `GetNewPropertyListings` |
| "Which 3 cities have the most listings?" | `GetTopCitiesByListings(3)` |
| "What is the total rent revenue collected?" | `GetPaymentRevenueSummary` |
| "How much revenue was collected in June 2025?" | `GetPaymentRevenueInRange` |
| "How many active subscriptions do we have?" | `GetSubscriptionOverview` |
| "Which subscription plan is most popular?" | `GetSubscriptionsByPlan` |
| "How many users signed up this month?" | `GetNewUsersInRange` |
| "Give me a full system overview" | Multiple functions called in sequence |
