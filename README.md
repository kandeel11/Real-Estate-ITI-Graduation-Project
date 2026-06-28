# Real Estate Management Platform

A production-grade **modular monolith** built on .NET 10 + Angular 18+, covering the complete lifecycle of property listings, tenant leases, maintenance tickets, subscription plans, and rent payments — with Stripe-hosted checkout, AI-powered routing, wallet ledgers, and real-time notifications.

> **ITI Graduation Project — 2026**

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Technology Stack](#2-technology-stack)
3. [Architecture](#3-architecture)
4. [Backend Modules](#4-backend-modules)
   - [Identity](#41-identity)
   - [Property](#42-property)
   - [Document (Leases)](#43-document-leases)
   - [Maintenance](#44-maintenance)
   - [Payment](#45-payment)
   - [Wallet](#46-wallet)
   - [Subscription](#47-subscription)
   - [AI](#48-ai)
5. [Payment Lifecycle — Complete](#5-payment-lifecycle--complete)
   - [Rent Payment (Stripe Checkout)](#51-rent-payment-stripe-checkout)
   - [Subscription Payment](#52-subscription-payment)
   - [Maintenance Payment](#53-maintenance-payment)
   - [Wallet Credit Routing](#54-wallet-credit-routing)
   - [Late Fee & Waive Late Fee](#55-late-fee--waive-late-fee)
6. [Background Jobs](#6-background-jobs)
7. [AI & Automation](#7-ai--automation)
8. [Dispute Tickets](#8-dispute-tickets)
9. [Frontend Dashboards](#9-frontend-dashboards)
10. [Caching Strategy](#10-caching-strategy)
11. [Cloud & CI/CD](#11-cloud--cicd)
12. [API Reference](#12-api-reference)
13. [Getting Started](#13-getting-started)
14. [Environment & Secrets](#14-environment--secrets)

---

## 1. Project Overview

The platform manages every interaction between **landlords (owners)**, **tenants**, **real estate brokers**, **property management companies**, and **maintenance vendors** in one unified system.

**Key differentiators:**

| Capability | Detail |
|---|---|
| Modular Monolith | 9 bounded modules; zero compile-time cross-module imports; integration via MediatR + Host adapters |
| Stripe Checkout | Rent paid via Stripe-hosted Checkout Sessions (auto-generated 2 days before due date) |
| 1% Platform Commission | Baked into every tenant-facing checkout amount; recorded on the payment entity |
| Wallet Ledger | Owner and company manager wallets credited 99% of rent on payment success via Outbox |
| Late Fee Waiver | Owner and company manager can cancel a late fee; Stripe link regenerated at base amount |
| AI Maintenance Routing | n8n + LLM determines liability; routes ticket to management company when applicable |
| Property Deduplication | Qdrant vector search detects duplicate property listings before submission |
| Dispute Tickets | Tenants and owners raise disputes; resolved through platform workflow |
| Real-time Notifications | SignalR hub for in-app notifications; SMTP for email |

---

## 2. Technology Stack

| Layer | Technology |
|---|---|
| **Runtime** | .NET 10 / ASP.NET Core |
| **ORM** | Entity Framework Core 10 |
| **Database** | SQL Server (Azure SQL in production) |
| **Cache** | Redis (Memurai on Windows dev / Azure Cache for Redis in production) |
| **Payments** | Stripe — Checkout Sessions + Webhooks + PaymentIntent |
| **AI / LLM** | Azure OpenAI Embeddings (`text-embedding-ada-002`) + Groq (`llama-3.3-70b`) via Semantic Kernel |
| **AI Workflows** | n8n webhooks — ticket evaluation + ownership verification |
| **Vector Store** | Qdrant — property semantic duplicate detection |
| **Document OCR** | Azure Document Intelligence |
| **File Storage** | Azure Blob Storage |
| **Email** | SMTP (Gmail dev / production SMTP) |
| **Real-time** | ASP.NET Core SignalR |
| **Messaging** | MediatR — CQRS + in-process INotification |
| **Frontend** | Angular 18+ standalone components, Signals, `@if`/`@for` control flow |
| **CI/CD** | GitHub Actions → Azure App Service |

---

## 3. Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                      RealEstate.Host                         │
│   Controllers  │  Adapters  │  Jobs  │  Behaviors           │
│   ─────────────────────────────────────────────────────────  │
│   MediatR Pipeline  (Logging → Caching → Auth → Handler)    │
└──────────────────────────────┬───────────────────────────────┘
                               │  IRequest / INotification
         ┌──────────┬──────────┼──────────┬──────────┬────────┐
         ▼          ▼          ▼          ▼          ▼        ▼
      Identity  Property  Maintenance  Payment  Subscription  AI
      Document   Wallet   MaintenanceCo
         │          │          │          │          │        │
         └──────────┴──────────┴──────────┴──────────┴────────┘
                    RealEstate.Shared.Kernel
            (base entities, integration events, enums)
```

### Cross-module communication rules

- Modules **never** reference each other at the project level.
- Queries that need data from another module use **Host-layer port/adapter** interfaces (e.g. `ILeaseService`, `IPropertyTitleService`, `IPropertyManagementChecker`).
- Long-running integration uses the **Outbox pattern** — integration events are persisted to `outbox_messages` and published by `OutboxPublisherJob` every 10 seconds, guaranteeing at-least-once delivery across restarts.

---

## 4. Backend Modules

### 4.1 Identity

`src/Modules/Identity/`

- Users, roles (`admin`, `owner`, `tenant`, `broker`, `company_manager`, `maintenance-company`)
- JWT authentication — access token + refresh token
- Company registration (company_manager role)
- Email OTP verification
- Password reset

**Key entities:** `ApplicationUser`, `Company`, `RefreshToken`

---

### 4.2 Property

`src/Modules/Property/`

- Full property lifecycle: draft → submitted → under-review → active / rejected
- Property media management (Azure Blob Storage)
- Amenities catalog
- AI-generated descriptions via Azure OpenAI
- **Qdrant duplicate detection** — embeddings of title + description + location checked against vector collection before submission; flags near-duplicates for admin review
- City/featured/search filters
- Broker assignment
- Full-management company assignment (`IsFullManagement`, `IsMaintenanceEnabled`)
- Saved properties (tenant wishlist)

**Key entities:** `Property`, `PropertyMedia`, `PropertyAmenity`, `PropertyManagementConfig`

**AI Deduplication flow:**

```
POST /api/properties/{id}/submit
        │
        ▼
QdrantPropertySeed checks vector similarity
  ├─ Score < threshold → mark IsDuplicate = false, proceed
  └─ Score ≥ threshold → flag DuplicateSuspected, admin reviews
        │
        ▼
Property.Submit() → status Active or UnderReview
```

---

### 4.3 Document (Leases)

`src/Modules/Document/`

- Lease creation with OCR verification (Azure Document Intelligence)
- Automated confidence scoring (auto-verify ≥ 85%, manual review 40–84%, reject < 40%)
- Digital signing workflow
- Azure Blob lease document storage with SAS URL generation
- Lease lifecycle: draft → pending-approval → awaiting-tenant-signature → active → expired/terminated
- Real-time notifications via SignalR when lease status changes

**Key entities:** `Lease`, `LeaseDocument`, `LeaseSignature`

**Adapter note:** The Payment module consumes lease data via `ILeaseService` (defined in `Payment.Application`) implemented by `DocumentLeaseServiceAdapter` in the Host. `ActiveLeaseDto` carries: tenant info, property ID, landlord ID, payment frequency, installment amount, and optional `CompanyId`.

---

### 4.4 Maintenance

`src/Modules/Maintenance/`

#### AI Ticket Routing

```
Tenant submits ticket
        │
        ▼
  ticket.Submit()  ──► validates image, assigns TicketNumber
        │
        ▼
  n8n AI Evaluation webhook
  ┌─ ResponsibleParty = Tenant ──► WaitingForTenantDecision
  └─ ResponsibleParty = Owner  ──► WaitingForOwnerDecision
                                        │
                                        ▼
                             IPropertyManagementChecker.GetAsync()
                             ┌─ no company / not full-managed ──► owner queue
                             └─ IsFullManagement = true
                                IsMaintenanceEnabled = true
                                        │
                                        ▼
                             MaintenanceTicket.CreateForCompany()
                             ticket.RouteToManagementCompany()
                             TicketRoutedToCompanyDomainEvent fired
```

#### Maintenance Company Assignment

- Vendor (maintenance company) sets price via `POST /api/maintenance/tickets/{id}/payments`
- Checkout URL generated (`POST /api/maintenance/tickets/{id}/payments/checkout`)
- Stripe Checkout Session carries `maintenance_payment_id` in metadata
- 2% platform commission recorded on `MaintenancePayment`
- On `checkout.session.completed` → `MaintenancePaymentConfirmedEvent` → ticket status = Completed

**Key entities:** `MaintenanceTicket`, `MaintenancePayment`, `TicketStatusHistory`, `TicketNote`

**Fields:**

| Field | Entity | Purpose |
|---|---|---|
| `RoutedToCompanyId` | `MaintenanceTicket` | Set when routed to management company |
| `ParentTicketId` | `MaintenanceTicket` | Company ticket → references original tenant ticket |
| `ResponsibleParty` | `MaintenanceTicket` | `Owner` or `Tenant` (set by AI) |
| `IsFullManagement` | `Property` | Company manages all operations |
| `IsMaintenanceEnabled` | `Property` | Maintenance specifically delegated to company |

---

### 4.5 Payment

`src/Modules/Payment/`

Central module for all financial operations. Four payment types: `Rent`, `Subscription`, `Transfer`, `Deposit`/`Withdrawal`.

#### Payment entity key fields

| Field | Purpose |
|---|---|
| `Amount` | Base rent amount (AED) |
| `LateFee` | Applied by OverduePaymentsJob if overdue |
| `CommissionAmount` | 1% platform commission — recorded on payment success |
| `CheckoutSessionId` | Stripe session ID — set by RentPaymentLinkJob |
| `CheckoutUrl` | Stripe-hosted checkout URL — sent to tenant |
| `Status` | `Pending` → `Late` → `Paid` / `Failed` / `Refunded` |
| `DueDate` | Per-period rent due date |
| `RentDetails` | 1:1 relation — LeaseId, PropertyId, TenantId, LandlordId |

#### Domain methods

```csharp
payment.MarkAsPaid(transactionId, method, receiptUrl)
payment.RecordCommission(rate = 0.01m)       // 1% platform fee
payment.ApplyLateFee(amount)                 // sets LateFee + status = Late
payment.WaiveLateFee()                       // sets LateFee = 0, restores Pending, clears checkout URL
payment.SetCheckoutSession(sessionId, url)   // stores Stripe checkout link
```

#### Stripe idempotency

`CreateCheckoutSessionAsync` uses `checkout_{paymentId}_{amountCents}{suffix}` as the idempotency key. Different amount = different key = fresh Stripe session. Suffix `_lf{days}` for overdue regeneration, `_wlf` for waived-fee regeneration.

---

### 4.6 Wallet

`src/Modules/Wallet/`

Every owner and company has a wallet. Rent payments credited automatically via the Outbox event pipeline.

#### Wallet routing

```
checkout.session.completed → RentPaymentSucceededEvent (Outbox)
        │
        ├─ CompanyId = null  ──► RentPaymentSucceededHandler
        │                        Owner wallet credited 99% of rent
        │
        └─ CompanyId ≠ null  ──► CompanyWalletRentCreditHandler
                                 Company wallet credited 99% of rent
```

Platform keeps 1% commission (the amount charged to the tenant is `base × 1.01`, so the platform earns exactly 1% of the base rent).

#### Ledger entry description format

```
Rent payment — property "Al Nakheel Tower" (due Jun 2025), lease {id} (net of 50.00 AED commission)
```

#### Wallet features

- Balance overview
- Paginated ledger (all credits/debits)
- Withdrawal requests (admin approve/reject)
- 1% withdrawal commission

**Key entities:** `Wallet`, `LedgerEntry`, `WithdrawalRequest`

**Idempotency:** `LedgerEntryExistsAsync(paymentId)` / `CompanyLedgerEntryExistsAsync(paymentId)` — duplicate credits are skipped even if the Outbox retries.

---

### 4.7 Subscription

`src/Modules/Subscription/`

- Subscription plans catalog (`GET /api/subscription-plans`)
- Owner/company subscribes to a plan (unlocks listing quota, advanced features)
- `SelectPlanCommand` creates a `PendingPayment` subscription
- Payment via Stripe Checkout Session (`POST /api/payments/subscriptions/checkout`)
- On `checkout.session.completed` → `SubscriptionPaymentSucceededEvent` (Outbox) → `subscription.Activate()`
- Success redirect calls `POST /api/payments/subscriptions/confirm` (confirms without relying on webhook being forwarded locally)

**Billing cycles:** Monthly (0), Quarterly (1), Annual (2)

---

### 4.8 AI

`src/Modules/AI/`

- **Chatbot** — Semantic Kernel + Groq (`llama-3.3-70b`), multi-turn conversations, sessions stored in Redis (30-min sliding TTL)
- **Property descriptions** — Azure OpenAI generates marketing copy from property attributes; owner accepts or re-generates
- **Qdrant property search** — semantic property search with embedding similarity; `QdrantPropertySeed` populates vector collection at startup
- **Admin analytics chat** — LLM-powered natural-language interface over platform data

---

## 5. Payment Lifecycle — Complete

### 5.1 Rent Payment (Stripe Checkout)

```
(Day -2 before due date)
RentPaymentLinkJob runs at 08:00 UTC
  → finds Pending rent payments without CheckoutUrl in [today, today+DaysBeforeDue]
  → for each: amount = (base + lateFee) × 1.01 (1% commission)
  → CreateCheckoutSessionAsync → Stripe-hosted page URL
  → payment.SetCheckoutSession(sessionId, url)

(Tenant clicks "Pay Now" in dashboard)
  → window.location.href = checkoutUrl
  → Stripe-hosted checkout page

(Stripe completes)
  → Stripe calls POST /api/payments/webhook/stripe
  → checkout.session.completed
  → payment.MarkAsPaid()
  → payment.RecordCommission()        ← 1% recorded
  → RentPaymentSucceededEvent → outbox
  → TrySendPaymentConfirmationAsync() ← tenant email

(Angular success page /dashboard/payments/success?session_id=...)
  → POST /api/payments/rent/confirm   ← idempotent fallback if webhook missed
  → shows confirmation to tenant

(OutboxPublisherJob — every 10 s)
  → publishes RentPaymentSucceededEvent
  → wallet handler credits owner or company wallet (99%)
```

**Cancel flow:** Stripe redirects to `/dashboard/payments/cancelled` — simple informational page, no charge made.

---

### 5.2 Subscription Payment

```
POST /api/subscriptions/select-plan      ← creates PendingPayment subscription
POST /api/payments/subscriptions/checkout ← Stripe Checkout URL
  → user redirected to Stripe
  → checkout.session.completed
  → SubscriptionPaymentSucceededEvent → outbox
  → subscription.Activate()

Success redirect: POST /api/payments/subscriptions/confirm (idempotent)
```

---

### 5.3 Maintenance Payment

```
POST /api/maintenance/tickets/{id}/payments         ← vendor sets price
POST /api/maintenance/tickets/{id}/payments/checkout ← Stripe Checkout URL
  → checkout.session.completed
  → metadata["maintenance_payment_id"] checked FIRST (before rent/subscription)
  → MaintenancePaymentConfirmedEvent (synchronous MediatR publish)
  → ticket.ConfirmPayment() → status = Completed
  → 2% commission recorded on MaintenancePayment
```

---

### 5.4 Wallet Credit Routing

| Condition | Handler | Amount |
|---|---|---|
| `CompanyId = null` (direct owner deal) | `RentPaymentSucceededHandler` | Owner wallet +99% |
| `CompanyId ≠ null` (company-managed deal) | `CompanyWalletRentCreditHandler` | Company wallet +99% |

CompanyId is resolved from Stripe session metadata (set by `RentPaymentLinkJob`) with fallback to `ILeaseService.GetLeaseByIdAsync`.

---

### 5.5 Late Fee & Waive Late Fee

#### Late fee application

```
OverduePaymentsJob runs at 09:00 UTC (after RentPaymentLinkJob at 08:00)
  → finds Pending rent past due date with LateFee = 0
  → calculates tiered fee (admin-configurable):
      ≤ GracePeriodDays   → 0%
      ≤ Tier1Days         → Tier1Percent% (default: 7 days → 2%)
      >  Tier1Days        → Tier2Percent% (default: 5%)
      cap at LateFeeMaxPercent (default: 15%)
  → payment.ApplyLateFee(fee) → status = Late
  → regenerates Stripe checkout link: (base + lateFee) × 1.01
     idempotency suffix: _lf{daysOverdue}
```

#### Configuration (`appsettings.json → Payment:OverdueJob`)

```json
{
  "Enabled": true,
  "SendSmsReminders": false,
  "LateFeeGracePeriodDays": 0,
  "LateFeeTier1Days": 7,
  "LateFeeTier1Percent": 2.0,
  "LateFeeTier2Percent": 5.0,
  "LateFeeMaxPercent": 15.0
}
```

#### Waive late fee

```
POST /api/payments/{id}/waive-late-fee
  Roles: owner, company_manager, admin
  
  WaiveLateFeeHandler:
    1. Load payment
    2. Verify: owner must be landlord of that payment (LandlordId == requesterId)
    3. payment.WaiveLateFee()
        → LateFee = 0
        → Status: Late → Pending
        → CheckoutUrl = null, CheckoutSessionId = null
    4. CreateCheckoutSessionAsync at base × 1.01
        → idempotency suffix: _wlf
    5. payment.SetCheckoutSession(newSessionId, newUrl)
    6. SaveChangesAsync
```

Tenant's "Pay Now" link is immediately updated to reflect the waived amount.

---

## 6. Background Jobs

| Job | Schedule | Purpose |
|---|---|---|
| `OutboxPublisherJob` | Every 10 s | Delivers pending integration events from `outbox_messages` via MediatR; 5 retries |
| `MonthlyPaymentGenerationJob` | Daily | Creates `Payment` records for rent installments due today (idempotent) |
| `RentPaymentLinkJob` | Daily 08:00 UTC | Generates Stripe Checkout links for rent payments due within `DaysBeforeDue` days |
| `OverduePaymentsJob` | Daily 09:00 UTC | Applies tiered late fees to overdue rent; regenerates Stripe checkout link |
| `LeaseExpiryBackgroundService` | Daily | Sends expiry notifications 30 days before lease end; marks expired leases |

#### `RentPaymentLinkJob` configuration (`Payment:RentLinkJob`)

```json
{
  "DaysBeforeDue": 2,
  "SuccessUrl": "http://localhost:4200/dashboard/payments/success",
  "CancelUrl":  "http://localhost:4200/dashboard/payments/cancelled"
}
```

---

## 7. AI & Automation

### Chatbot (Semantic Kernel + Groq)

- Multi-turn conversations with Redis session persistence (30-min sliding TTL)
- Real estate domain prompt engineering
- Session history: `GET /api/AI/chatbot/sessions/{sessionId}/history`

### Property Duplicate Detection (Qdrant)

```
QdrantPropertySeed (startup):
  → loads all active properties from DB
  → generates Azure OpenAI embeddings (text-embedding-ada-002)
  → upserts into Qdrant collection "properties"

POST /api/properties/{id}/submit:
  → IPropertyDuplicateCheckService.UpsertDirectAsync(property)
  → cosine similarity check against existing embeddings
  → flags DuplicateSuspected if score ≥ threshold
```

### AI Ticket Evaluation (n8n)

- n8n workflow at `https://mkhaled2626.app.n8n.cloud/webhook/ticket-evaluate`
- Receives: ticket description, images, property context
- Returns: `ResponsibleParty` (`Tenant` / `Owner`), confidence score, reasoning
- Result stored on ticket; drives routing decision

### Admin Analytics Chat

- LLM natural-language interface over platform financial and operational data
- `POST /api/AI/admin/analytics/chat`

---

## 8. Dispute Tickets

Tenants and owners can open formal disputes through the platform.

```
POST /api/disputes           ← open dispute (tenant or owner)
GET  /api/disputes/{id}      ← dispute details
GET  /api/disputes/mine      ← my disputes
PATCH /api/disputes/{id}/resolve ← resolve (admin)
```

**Dispute entity fields:** `DisputeType`, `Status` (`Open` / `UnderReview` / `Resolved` / `Closed`), `Description`, `Resolution`, `RaisedBy`, `RelatedPaymentId?`, `RelatedLeaseId?`

Disputes are visible in the admin dashboard with full details and status management.

---

## 9. Frontend Dashboards

The Angular frontend is organized into role-specific dashboards, each as a standalone lazy-loaded shell component.

### Routes summary

| Path | Role | Description |
|---|---|---|
| `/dashboard/owner` | `owner` | Owner dashboard shell |
| `/dashboard/owner/payments` | `owner` | Rent payments + waive late fee |
| `/dashboard/owner/wallet` | `owner` | Wallet balance, ledger, withdrawals |
| `/dashboard/owner/leases` | `owner` | Active leases |
| `/dashboard/owner/maintenance` | `owner` | Maintenance tickets |
| `/dashboard/company` | `company_manager` | Company dashboard shell |
| `/dashboard/company/payments` | `company_manager` | Company rent payments + waive late fee |
| `/dashboard/company/wallet` | `company_manager` | Company wallet |
| `/dashboard/company/leases` | `company_manager` | Managed leases |
| `/dashboard/tenant` | `tenant` | Tenant dashboard shell |
| `/dashboard/payments` | `tenant` | Rent payment history + pay now |
| `/dashboard/payments/success` | `tenant` | Stripe success redirect confirmation |
| `/dashboard/payments/cancelled` | `tenant` | Stripe cancel redirect page |
| `/dashboard/broker` | `broker` | Broker dashboard |
| `/dashboard/admin` | `admin` | Admin dashboard |
| `/dashboard/maintenance` | `maintenance-company` | Vendor dashboard |

### Tenant payments page

- Shows all rent periods with: Amount | Late Fee | Platform Fee (1%) | Total Charged | Status
- **"Pay Now"** button — redirects to Stripe-hosted checkout URL (pre-generated by job)
- **"Link pending"** chip — displayed when checkout link not yet generated (< 2 days before due date)
- **Payment success page** — reads `?session_id=` from URL, calls `POST /api/payments/rent/confirm`, shows confirmation

### Owner payments page

- Shows all incoming rent across owned properties
- Summary cards: Total Collected / Pending / Late / Total Records
- **"Waive Fee"** button — appears for `Late` / `Overdue` payments with `lateFee > 0`
- Clicking waive: confirm dialog → `POST /api/payments/{id}/waive-late-fee` → list reloads with updated amounts

### Company payments page

- Same waive fee flow as owner
- Property breakdown by portfolio
- AED currency throughout

### Company & Owner wallet pages

- Balance card
- Paginated ledger with property/lease references in descriptions
- Withdrawal request flow

---

## 10. Caching Strategy

### Query Cache — `CachingBehavior`

MediatR `IPipelineBehavior` — checks Redis before handler, writes result on miss.

**Key format:** `q:{QueryTypeName}:{MD5(JSON-serialized-params)}`

| Query | TTL |
|---|---|
| `GetPropertiesByCompanyQuery` | 60 s |
| `GetCompanyBrokersDetailsQuery` | 120 s |
| `GetCompanyDetailsQuery` | 5 min |
| `GetLeasesByPropertyIdsQuery` | 60 s |
| `GetPendingApprovalLeasesQuery` | 30 s |

Redis failures are non-fatal — cache error falls through to the database.

### AI Session Store

`AI.Infrastructure/Services/RedisSessionStore.cs` — implements `ISessionStore` via `IDistributedCache`. Keys: `ai:session:{guid}`, 30-minute sliding TTL.

---

## 11. Cloud & CI/CD

### Azure infrastructure

| Service | Purpose |
|---|---|
| Azure App Service (`realestate-ai-app`) | Hosts the .NET 10 API |
| Azure SQL Database | Primary relational store |
| Azure Cache for Redis | Distributed query cache + AI sessions |
| Azure Blob Storage | Lease documents, property images |
| Azure Document Intelligence | OCR for lease verification |
| Azure OpenAI | Text embeddings |

### GitHub Actions pipeline

**Trigger:** push to `main` or `workflow_dispatch`

```
build:
  setup .NET 10
  dotnet build --configuration Release
  dotnet publish -c Release -o $DOTNET_ROOT/myapp
  upload artifact

deploy: (needs: build)
  azure/login  (OIDC — no stored passwords)
  azure/webapps-deploy → app: realestate-ai-app
```

Authentication uses federated OIDC credentials (`CLIENTID`, `TENANTID`, `SUBSCRIPTIONID` secrets) — no service-principal passwords stored.

---

## 12. API Reference

### Authentication

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/auth/login` | Login → JWT access token + refresh token |
| `POST` | `/api/auth/register` | Register user |
| `POST` | `/api/auth/refresh` | Refresh access token |
| `POST` | `/api/auth/verify-otp` | Verify email OTP |
| `POST` | `/api/auth/forgot-password` | Send reset link |

### Properties

| Method | Route | Roles | Description |
|---|---|---|---|
| `GET` | `/api/properties/active` | Public | Browse active listings |
| `GET` | `/api/properties/mine` | owner, broker | My listings |
| `POST` | `/api/properties` | owner, broker | Create property |
| `POST` | `/api/properties/{id}/submit` | owner, broker | Submit for review |
| `POST` | `/api/properties/{id}/request-ai-description` | owner, broker | Generate AI description |

### Maintenance Tickets

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/maintenance/tickets` | Create ticket (tenant) |
| `POST` | `/api/maintenance/tickets/{id}/submit` | Submit + trigger AI evaluation |
| `GET` | `/api/maintenance/tickets/{id}/ai-evaluation` | Poll AI result |
| `POST` | `/api/maintenance/tickets/{id}/decision` | Owner/tenant decision |
| `POST` | `/api/maintenance/tickets/{id}/assign-vendor` | Assign vendor |
| `POST` | `/api/maintenance/tickets/{id}/payments` | Vendor sets price |
| `POST` | `/api/maintenance/tickets/{id}/payments/checkout` | Get Stripe URL |
| `GET` | `/api/maintenance/tickets/company/{id}/routed` | Routed tickets (company) |

### Payments

| Method | Route | Roles | Description |
|---|---|---|---|
| `GET` | `/api/payments/my` | all | My payments (role-aware) |
| `POST` | `/api/payments/rent/confirm` | tenant | Confirm rent checkout session |
| `POST` | `/api/payments/{id}/waive-late-fee` | owner, company_manager, admin | Waive late fee |
| `POST` | `/api/payments/{id}/late-fee` | admin | Manually apply late fee |
| `POST` | `/api/payments/{id}/refund` | admin | Refund payment |
| `POST` | `/api/payments/subscriptions/checkout` | owner, company_manager | Stripe checkout for subscription |
| `POST` | `/api/payments/subscriptions/confirm` | owner, company_manager | Confirm subscription session |
| `GET` | `/api/payments/platform-dashboard` | admin | Platform financial summary |
| `POST` | `/api/payments/webhook/stripe` | — | Stripe webhook receiver |

### Wallet

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/wallet/balance` | Current balance |
| `GET` | `/api/wallet/ledger` | Paginated ledger entries |
| `POST` | `/api/wallet/withdrawals` | Request withdrawal |
| `GET` | `/api/wallet/withdrawals` | My withdrawal requests |
| `POST` | `/api/wallet/withdrawals/{id}/approve` | Admin approve |
| `POST` | `/api/wallet/withdrawals/{id}/reject` | Admin reject |

### Disputes

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/disputes` | Open a dispute |
| `GET` | `/api/disputes/mine` | My disputes |
| `GET` | `/api/disputes/{id}` | Dispute details |
| `PATCH` | `/api/disputes/{id}/resolve` | Resolve (admin) |

---

## 13. Getting Started

### Prerequisites

- .NET 10 SDK
- SQL Server or SQL Server Express
- Redis (Memurai recommended on Windows)
- Stripe test account
- Qdrant instance (cloud or local Docker)

### 1. Clone and restore

```bash
git clone https://github.com/kandeel11/Real-Estate-ITI-Graduation-Project.git
cd Real-Estate-ITI-Graduation-Project
dotnet restore
```

### 2. Set local secrets

```bash
cd src/Host/RealEstate.Host

dotnet user-secrets set "Stripe:SecretKey"        "sk_test_..."
dotnet user-secrets set "Stripe:WebhookSecret"    "whsec_..."
dotnet user-secrets set "AI:GroqApiKey"           "gsk_..."
dotnet user-secrets set "AI:AzureEmbeddingApiKey" "..."
dotnet user-secrets set "Qdrant:ApiKey"           "..."
```

### 3. Apply database migrations

```bash
dotnet ef database update \
  --project src/Modules/Identity/Identity.Infrastructure \
  --startup-project src/Host/RealEstate.Host

dotnet ef database update \
  --project src/Modules/Property/Property.Infrastructure \
  --startup-project src/Host/RealEstate.Host

dotnet ef database update \
  --project src/Modules/Maintenance/Maintenance.Infrastructure \
  --startup-project src/Host/RealEstate.Host

dotnet ef database update \
  --project src/Modules/Payment/Payment.Infrastructure \
  --startup-project src/Host/RealEstate.Host

dotnet ef database update \
  --project src/Modules/Document/Document.Infrastructure \
  --startup-project src/Host/RealEstate.Host

dotnet ef database update \
  --project src/Modules/Subscription/Subscription.Infrastructure \
  --startup-project src/Host/RealEstate.Host

dotnet ef database update \
  --project src/Modules/Wallet/Wallet.Infrastructure \
  --startup-project src/Host/RealEstate.Host
```

### 4. Run the API

```bash
cd src/Host/RealEstate.Host
dotnet run
```

Swagger: `https://localhost:7240/swagger`

### 5. Run the frontend

```bash
cd frontend
npm install
ng serve
```

App: `http://localhost:4200`

### 6. Stripe webhook forwarding (local dev)

```bash
stripe listen --forward-to https://localhost:7240/api/payments/webhook/stripe
```

---

## 14. Environment & Secrets

All sensitive values are **never committed**. Local dev uses `dotnet user-secrets`; production uses Azure App Service Application Settings.

| Key | Description |
|---|---|
| `ConnectionStrings:DefaultConnection` | SQL Server connection string |
| `Stripe:SecretKey` | Stripe secret key (`sk_test_...`) |
| `Stripe:PublishableKey` | Stripe publishable key |
| `Stripe:WebhookSecret` | Stripe webhook signing secret (`whsec_...`) |
| `AI:GroqApiKey` | Groq API key for LLM chat |
| `AI:AzureEmbeddingApiKey` | Azure OpenAI embedding key |
| `AI:AzureEmbeddingEndpoint` | Azure OpenAI endpoint |
| `Qdrant:ApiKey` | Qdrant cloud API key |
| `Qdrant:Endpoint` | Qdrant cluster host |
| `EmailSettings:Password` | SMTP password |
| `Jwt:SigningKey` | JWT HMAC signing key (min 32 chars) |

---

_ITI Graduation Project · Class of 2026_
