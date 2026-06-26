# Real Estate Management Platform — AI-Powered

A production-grade, modular monolith built on **.NET 10** with intelligent ticket routing, Stripe payments, AI-driven property analysis, and real-time notifications.

---

## Table of Contents

1. [Project Overview](#project-overview)
2. [Technology Stack](#technology-stack)
3. [Architecture](#architecture)
4. [Module Breakdown](#module-breakdown)
5. [System Behavior: AI Ticket Routing](#system-behavior-ai-ticket-routing)
6. [System Behavior: Payment Lifecycle](#system-behavior-payment-lifecycle)
7. [Background Jobs](#background-jobs)
8. [Caching Strategy](#caching-strategy)
9. [Cloud & CI/CD (Azure)](#cloud--cicd-azure)
10. [Getting Started](#getting-started)
11. [API Reference Highlights](#api-reference-highlights)

---

## Project Overview

A full-featured real estate platform that manages the complete lifecycle of property listings, tenant leases, maintenance tickets, and payments. Key differentiators:

- **Intelligent Maintenance Routing** — AI automatically determines liability (tenant vs. landlord) and routes tickets to property management companies when applicable.
- **Multi-purpose Stripe Integration** — PaymentIntent for rent (custom checkout UI), Checkout Sessions for subscriptions and maintenance fees (Stripe-hosted page).
- **Modular Monolith** — Strongly bounded modules (Property, Identity, Maintenance, Payment, AI, Document, Subscription) with zero compile-time cross-module dependencies; integration via MediatR notifications and Host-layer adapters.
- **Outbox Pattern** — Guaranteed-delivery integration events for payment confirmations and subscription activations.
- **Redis Distributed Cache** — Query-level TTL caching for high-read paths and AI chat session persistence.

---

## Technology Stack

| Layer        | Technology                                                          |
| ------------ | ------------------------------------------------------------------- |
| Runtime      | .NET 10 / ASP.NET Core                                              |
| ORM          | Entity Framework Core 10                                            |
| Database     | SQL Server (Azure SQL in production)                                |
| Cache        | Redis (Memurai on Windows dev, Azure Cache for Redis in production) |
| Payments     | Stripe (PaymentIntent + Checkout Sessions + Webhooks)               |
| AI / LLM     | Azure OpenAI Embeddings + Groq (llama-3.3-70b) via Semantic Kernel  |
| AI Workflows | n8n webhook (`ticket-evaluate` + ownership verification)            |
| Vector Store | Qdrant (property semantic search)                                   |
| Document OCR | Azure Document Intelligence                                         |
| File Storage | Azure Blob Storage                                                  |
| Email        | SMTP via Gmail (dev) / production SMTP                              |
| Messaging    | MediatR (CQRS + in-process notifications)                           |
| Frontend     | Angular (separate repository)                                       |
| CI/CD        | GitHub Actions → Azure App Service                                  |

---

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    RealEstate.Host                       │
│  Controllers  │  Adapters  │  Jobs  │  Behaviors        │
│  ─────────────────────────────────────────────────────  │
│  MediatR Pipeline (Auth → Caching → Handler)            │
└──────────────────────────────┬──────────────────────────┘
                               │ IRequest / INotification
    ┌──────────┬───────────────┼──────────┬──────────────┐
    ▼          ▼               ▼          ▼              ▼
 Identity  Property      Maintenance  Payment      Subscription
 .App      .App          .App         .App         .App
 .Infra    .Infra        .Infra       .Infra       .Infra
    │          │               │          │              │
    └──────────┴───────────────┴──────────┴──────────────┘
                    Shared.Kernel (events, base entities)
```

**Cross-module communication rules:**

- Modules do **not** reference each other at the project level.
- Queries that span modules use **Host-layer adapters** (e.g. `PropertyManagementCheckerAdapter`) that implement interfaces defined in the consuming module.
- Long-running integration (e.g. subscription activation after payment) uses the **Outbox pattern** so consistency is guaranteed even if the downstream handler is temporarily unavailable.

---

## Module Breakdown

| Module            | Responsibility                                                         |
| ----------------- | ---------------------------------------------------------------------- |
| **Identity**      | Users, companies, authentication (JWT), roles                          |
| **Property**      | Listings, verification, AI duplicate detection, full-management config |
| **Maintenance**   | Tickets, AI evaluation, vendor assignment, payments, routing           |
| **Payment**       | Rent & subscription payments, Stripe integration, outbox events        |
| **Subscription**  | Company subscription plans, activation via outbox event                |
| **Document**      | Lease contracts, OCR verification, digital signing                     |
| **AI**            | Chat assistant (Semantic Kernel), property QA, session storage         |
| **Shared.Kernel** | Base entities, domain events, integration events                       |

---

## System Behavior: AI Ticket Routing

### Overview

When a tenant submits a maintenance ticket the system runs a fully automated pipeline:

```
Tenant submits ticket
        │
        ▼
  ticket.Submit()  ──► validates image attachment, assigns TicketNumber
        │
        ▼
  AI Evaluation  (n8n → LLM webhook)
  ┌─ ResponsibleParty = Tenant ──► WaitingForTenantDecision
  └─ ResponsibleParty = Owner  ──► WaitingForOwnerDecision
                                         │
                                         ▼
                              IPropertyManagementChecker.GetAsync()
                              ┌─ No management / not full-management ──► normal owner flow
                              └─ IsFullManagement=true & IsMaintenanceEnabled=true
                                         │
                                         ▼
                              MaintenanceTicket.CreateForCompany()
                              (company-facing shadow ticket, OwnerId = CompanyManager)
                                         │
                                         ▼
                              ticket.RouteToManagementCompany()
                              (fires TicketRoutedToCompanyDomainEvent)
```

### Key Rules

- **AI determines liability** by analyzing the ticket description and attached images via an n8n automation webhook at `https://mkhaled2626.app.n8n.cloud/webhook/ticket-evaluate`.
- **Routing condition**: Property has `CompanyId` **AND** `IsFullManagement = true` **AND** `IsMaintenanceEnabled = true` **AND** the company is verified and active.
- The original tenant ticket is stamped with `RoutedToCompanyId`; the company ticket stores `ParentTicketId` pointing back to the original.
- The company manager's queue is accessible via `GET /api/maintenance/tickets/company/{companyId}/routed`.
- Routing errors are non-fatal — the ticket falls through to the normal landlord queue if routing fails.

### Entities

| Field                  | Entity              | Purpose                                          |
| ---------------------- | ------------------- | ------------------------------------------------ |
| `RoutedToCompanyId`    | `MaintenanceTicket` | Set on tenant ticket when routed                 |
| `ParentTicketId`       | `MaintenanceTicket` | Set on company ticket, references original       |
| `ResponsibleParty`     | `MaintenanceTicket` | `Owner` or `Tenant` (set by AI)                  |
| `IsFullManagement`     | `Property`          | Company manages all operations for this property |
| `IsMaintenanceEnabled` | `Property`          | Maintenance specifically is delegated to company |

---

## System Behavior: Payment Lifecycle

### Rent Payment (PaymentIntent flow)

```
MonthlyPaymentGenerationJob creates payment record each month
        │
        ▼
POST /api/payments/{id}/initiate  ──► Stripe PaymentIntent → clientSecret returned
        │  (Angular frontend renders Stripe Elements form)
        ▼
Stripe calls POST /api/stripe/webhook: payment_intent.succeeded
        │
        ▼
  payment.MarkAsPaid(transactionId)
  payment.RecordCommission()   ← 1% platform fee
  Queues RentPaymentSucceededEvent → outbox_messages table
  Email confirmation sent to tenant
```

### Subscription Payment (Checkout Session flow)

```
POST /api/subscriptions/checkout  ──► Stripe-hosted Checkout page URL
        │  (frontend redirects user)
        ▼
Stripe calls webhook: checkout.session.completed
        │  metadata["payment_id"] checked
        ▼
  payment.MarkAsPaid()
  Queues SubscriptionPaymentSucceededEvent → outbox
        │
        ▼  (OutboxPublisherJob, every 10 s)
  Subscription module: subscription.Activate()
```

### Maintenance Payment (Checkout Session flow)

```
Vendor:  POST /api/maintenance/tickets/{id}/payments        ← sets price
Payer:   POST /api/maintenance/tickets/{id}/payments/checkout ← gets Stripe URL
        │  metadata["maintenance_payment_id"] = paymentId
        ▼
Stripe calls webhook: checkout.session.completed
        │  maintenance_payment_id checked FIRST
        ▼
  _publisher.Publish(MaintenancePaymentConfirmedEvent)  ← synchronous MediatR
        │
        ▼
  HandleMaintenancePaymentConfirmedHandler
    payment.TryConfirmViaWebhook(stripeTransactionId)   ← idempotent
    ticket.ConfirmPayment()  ──► Status = Completed
```

### Stripe Metadata Keys

| Key                      | Flow                | Consumer                              |
| ------------------------ | ------------------- | ------------------------------------- |
| `payment_id`             | Rent / Subscription | `Payment.Infrastructure`              |
| `maintenance_payment_id` | Maintenance fee     | `Maintenance.Application` (via event) |
| `user_id`                | Subscription        | `Payment.Infrastructure` (activation) |

---

## Background Jobs

### `OutboxPublisherJob`

**`Payment.Infrastructure/Outbox/OutboxPublisherJob.cs`**

Polls `outbox_messages` every **10 seconds**. Deserializes pending `INotification` records, publishes via MediatR, and marks each message `Processed`. Failed deliveries increment `RetryCount` (max 5 retries). This guarantees at-least-once delivery of `RentPaymentSucceededEvent` and `SubscriptionPaymentSucceededEvent` across restarts.

### `MonthlyPaymentGenerationJob`

**`Payment.Infrastructure/BackgroundJobs/MonthlyPaymentGenerationJob.cs`**

Runs **daily**. Reads all active leases and creates `Payment` records for installments due today. Idempotent — skips leases that already have a payment for the current period.

### `OverduePaymentsJob`

**`Payment.Infrastructure/BackgroundJobs/OverduePaymentsJob.cs`**

Runs **daily**. Marks unpaid rent past the due date as `Overdue`. Optionally sends SMS reminders controlled by `"OverdueJob:SendSmsReminders": true/false`.

### `LeaseExpiryBackgroundService`

**`Host/RealEstate.Host/Jobs/LeaseExpiryBackgroundService.cs`**

Runs **daily**. Fires expiry notifications for leases within 30 days of end date and marks expired leases.

---

## Caching Strategy

### Query Cache — `CachingBehavior`

**`Host/RealEstate.Host/Behaviors/CachingBehavior.cs`**

MediatR `IPipelineBehavior` that intercepts registered read queries, checks Redis first, and caches handler results on miss.

**Cache key format:** `q:{QueryTypeName}:{MD5(JSON-serialized-params)}`

| Query                           | TTL   |
| ------------------------------- | ----- |
| `GetPropertiesByCompanyQuery`   | 60 s  |
| `GetCompanyBrokersDetailsQuery` | 120 s |
| `GetCompanyDetailsQuery`        | 5 min |
| `GetLeasesByPropertyIdsQuery`   | 60 s  |
| `GetPendingApprovalLeasesQuery` | 30 s  |
| `GetAwaitingTenantLeasesQuery`  | 30 s  |

Redis failures are **non-fatal** — a cache error falls through to the database automatically.

### AI Session Store — `RedisSessionStore`

**`AI.Infrastructure/Services/RedisSessionStore.cs`**

Implements Semantic Kernel's `ISessionStore` via `IDistributedCache`. Keys: `ai:session:{guid}`, sliding **30-minute** expiry. Enables stateful multi-turn AI conversations without in-memory session state.

### Configuration

```json
"Redis": {
  "ConnectionString": "localhost:6379"
}
```

---

## Cloud & CI/CD (Azure)

### Azure Infrastructure

| Service                                     | Purpose                                                       |
| ------------------------------------------- | ------------------------------------------------------------- |
| **Azure App Service** (`realestate-ai-app`) | Hosts the .NET 10 API                                         |
| **Azure SQL Database**                      | Primary relational store (per-module schemas in one database) |
| **Azure Cache for Redis**                   | Distributed query cache + AI session store                    |
| **Azure Blob Storage**                      | Lease documents, property images, attachments                 |
| **Azure Document Intelligence**             | OCR for lease document verification                           |
| **Azure OpenAI**                            | Text embeddings (`text-embedding-ada-002`)                    |

### CI/CD Pipeline

**File:** `.github/workflows/main_realestate-ai-app.yml`  
**Trigger:** Push to `main` or manual `workflow_dispatch`

```
build job
  ├─ checkout
  ├─ setup .NET 10
  ├─ dotnet build --configuration Release
  ├─ dotnet publish -c Release -o $DOTNET_ROOT/myapp
  └─ upload artifact

deploy job  (needs: build)
  ├─ download artifact
  ├─ azure/login (OIDC — no stored passwords)
  └─ azure/webapps-deploy → app: realestate-ai-app, slot: Production
```

Authentication uses federated OIDC credentials (three GitHub secrets: `CLIENTID`, `TENANTID`, `SUBSCRIPTIONID`) — no stored service-principal passwords.

### Secrets Management

All sensitive values (`Stripe:SecretKey`, `Stripe:WebhookSecret`, AI API keys, connection strings) are stored as **Azure App Service Application Settings** (environment variables at runtime). They are never committed to the repository. Local development uses `dotnet user-secrets`.

---

## Getting Started

### Prerequisites

- .NET 10 SDK
- SQL Server / SQL Server Express
- Memurai or Redis (for caching)
- Stripe test account

### Local Secrets

```bash
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..."
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_..."
dotnet user-secrets set "AI:GroqApiKey" "gsk_..."
```

### Database Setup

```bash
dotnet ef database update --project src/Modules/Identity/Identity.Infrastructure       --startup-project src/Host/RealEstate.Host
dotnet ef database update --project src/Modules/Property/Property.Infrastructure       --startup-project src/Host/RealEstate.Host
dotnet ef database update --project src/Modules/Maintenance/Maintenance.Infrastructure --startup-project src/Host/RealEstate.Host
dotnet ef database update --project src/Modules/Payment/Payment.Infrastructure         --startup-project src/Host/RealEstate.Host
dotnet ef database update --project src/Modules/Document/Document.Infrastructure       --startup-project src/Host/RealEstate.Host
```

### Run

```bash
cd src/Host/RealEstate.Host
dotnet run
```

Swagger UI: `https://localhost:7240/swagger`

---

## API Reference Highlights

### Maintenance Tickets

| Method | Route                                                 | Description                            |
| ------ | ----------------------------------------------------- | -------------------------------------- |
| `POST` | `/api/maintenance/tickets`                            | Create ticket (tenant only)            |
| `POST` | `/api/maintenance/tickets/{id}/submit`                | Submit + trigger AI evaluation         |
| `GET`  | `/api/maintenance/tickets/{id}/ai-evaluation`         | Poll AI result                         |
| `POST` | `/api/maintenance/tickets/{id}/decision`              | Owner/tenant chooses resolution method |
| `POST` | `/api/maintenance/tickets/{id}/assign-vendor`         | Assign maintenance company             |
| `POST` | `/api/maintenance/tickets/{id}/payments`              | Vendor sets price                      |
| `POST` | `/api/maintenance/tickets/{id}/payments/checkout`     | Generate Stripe Checkout URL           |
| `GET`  | `/api/maintenance/tickets/company/{companyId}/routed` | Company sees auto-routed tickets       |

### Payments

| Method | Route                            | Description                   |
| ------ | -------------------------------- | ----------------------------- |
| `POST` | `/api/payments/{id}/initiate`    | Create PaymentIntent for rent |
| `POST` | `/api/stripe/webhook`            | Stripe webhook receiver       |
| `GET`  | `/api/payments/checkout/success` | Success-redirect confirmation |

---

_ITI Graduation Project — 2026_
