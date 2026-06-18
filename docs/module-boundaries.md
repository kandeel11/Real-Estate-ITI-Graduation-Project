# Module Boundaries

## Overview

This document defines what each module owns, what it must not own, current boundary violations (smells), and the cross-module communication contracts needed for the wallet system.

---

## Payment Module (Existing)

### Owns
- External payment orchestration (Stripe PaymentIntent, Checkout Session)
- Stripe webhook processing (`StripeWebhookService`)
- Payment records — immutable audit trail of every financial transaction
- Outbox infrastructure (`OutboxWriter`, `OutboxPublisherJob`)
- Background jobs: `MonthlyPaymentGenerationJob`, `OverduePaymentsJob`
- Payment entity factory methods: `CreateRent`, `CreateSubscription`, `CreateTransfer`, `CreateDeposit`, `CreateWithdrawal`, `CreateRefund`
- Stripe settings, checkout service, gateway abstraction

### Must NOT Own (Going Forward)
- Wallet balance state
- Ledger / transaction history for wallet purposes
- Commission calculation logic
- Withdrawal processing business rules
- Any knowledge of which actor "has money"

### Current Smells

**Smell 1: PaymentType.Deposit, PaymentType.Withdrawal, PaymentType.Transfer**
These three types conceptually belong to the Wallet domain's language, not Stripe's language. They exist in Payment because the original design made Payment the catch-all for all money movement. Under the Hybrid option, they remain in Payment as audit record types — but the business logic driving them lives in Wallet.

**Smell 2: PaymentMethod.Wallet has no implementation**
`PaymentMethod.Wallet = 3` exists in the enum with zero handler, zero service, zero logic behind it. This is a compile-time placeholder. The Wallet module will fill it by publishing `WalletTransferRecordedEvent`, which Payment handles by creating a `Payment` with `PaymentMethod.Wallet`.

**Smell 3: No OwnerId on Payment entity**
`Payment` does not have a direct `OwnerId` field. The owner is reachable via `RentPaymentDetails.LandlordId`. The `RentPaymentSucceededEvent` must explicitly carry `OwnerId` — do not force Wallet module to join Payment tables.

---

## Wallet Module (New)

### Owns
- `WalletBalance` aggregate root — the authoritative source of a user's available balance
- `LedgerEntry` — append-only record of every credit/debit against a wallet
- `WithdrawalRequest` aggregate — lifecycle of a payout request (Pending → Processing → Completed / Failed)
- Internal transfer logic (Wallet-to-Wallet)
- Balance invariants: balance cannot go negative without escrow release

### Must NOT Own
- Stripe client, API keys, webhook signatures
- External payment creation
- Lease data or lease business rules
- Subscription data

### Consumes (via Integration Events)
- `RentPaymentSucceededEvent` → credits owner wallet (**must be added to SharedKernel**)
- `RentPaymentRefundedEvent` → debits owner wallet if previously credited (**must be added to SharedKernel**)

### Publishes (via Integration Events)
- `WalletCreditedEvent` → Notification module (send owner "your wallet was credited AED X")
- `WithdrawalRequestedEvent` → Payment module (create audit `Payment` record with `PaymentType.Withdrawal`)
- `WalletTransferRecordedEvent` → Payment module (create audit `Payment` record with `PaymentType.Transfer`, `PaymentMethod.Wallet`)

---

## Lease Module (Existing, via ILeaseService)

### Owns
- Lease lifecycle (creation, activation, renewal, termination)
- Rent schedule (amount, payment day of month, start/end dates)
- `LeaseDto`: Id, PropertyId, TenantId, OwnerId, RentAmount, PaymentDayOfMonth, StartDate, EndDate, IsActive

### Must NOT Own
- Payment processing
- Wallet state
- Stripe integration

### Boundary Notes
- Payment module calls `ILeaseService.GetLeaseByIdAsync()` only for notification purposes (getting tenant email/name). This is acceptable.
- Wallet module must NOT call `ILeaseService` directly — it receives `OwnerId` via integration events, not by querying the Lease module.

---

## Maintenance Module (Existing)

### Owns
- `MaintenanceTicket` aggregate — full ticket lifecycle from Draft to Completed
- `TicketVendorAssignment` — vendor assignment and acceptance
- `TicketAiEvaluation` — AI assessment of responsibility
- `TicketStatusHistory` — audit trail of status transitions
- File attachments
- AI evaluation service

### Must NOT Own
- **`MaintenancePayment`** — THIS IS A SMELL (see below)

### Critical Architectural Smell: MaintenancePayment

`MaintenanceTicket` has a `MaintenancePayment` navigation property. `IMaintenancePaymentRepository` exists. `TicketStatus.AwaitingPayment` drives a payment confirmation flow inside the Maintenance module.

**Why this is wrong:**
- It creates a parallel payment system that bypasses the Payment module entirely
- There is no integration with `WalletBalance` — owner balance is not debited
- There is no audit record in `payment.payments` table
- `MaintenancePaymentStatus` enum (`MaintenancePaymentStatus.cs`) duplicates concepts already in `PaymentStatus`

**Correct flow (Phase 2):**
```
Ticket reaches AwaitingPayment
  → Owner triggers payment via Wallet module
  → Wallet module: WalletBalance(owner).Debit(amount) + LedgerEntry
  → Wallet module publishes WalletTransferRecordedEvent(ownerId, vendorId, amount, ticketId)
  → Payment module creates Payment.CreateTransfer() audit record
  → Wallet module publishes MaintenancePaymentConfirmedEvent
  → Maintenance module: ticket.ConfirmPayment()
```

**Phase 1 action:** Do not touch `MaintenancePayment`. Document the smell. Migrate in Phase 2 when vendor wallets exist.

---

## Notification Module (Existing)

### Owns
- Email delivery
- Push notification delivery
- Notification templates

### Must NOT Own
- Any business logic
- Payment or wallet state

### Boundary
- Receives `WalletCreditedEvent` from Wallet module and sends email to owner
- Receives existing payment confirmation events and sends emails (already implemented in `StripeWebhookService` via `INotificationService`)

---

## Subscription Module (Existing)

### Owns
- Subscription plans, features, billing cycles
- Subscription lifecycle (PendingPayment → Active → Cancelled)

### Boundary with Payment
- Payment publishes `SubscriptionPaymentSucceededEvent` (already in SharedKernel)
- Subscription module subscribes → `Subscription.Activate()`
- Wallet module has NO involvement in subscription payments (subscription fees go to platform, not to owners)

---

## Cross-Module Communication — Integration Events

### Already in SharedKernel
| Event | Publisher | Subscriber | Purpose |
|---|---|---|---|
| `SubscriptionPaymentSucceededEvent` | Payment | Subscription | Activate subscription after Stripe confirms |
| `LeaseEndedEvent` | Document | Property | Return property to market |

### Must Be Added to SharedKernel
| Event | Publisher | Subscriber | Purpose |
|---|---|---|---|
| `RentPaymentSucceededEvent` | Payment | Wallet | Credit owner wallet |
| `RentPaymentRefundedEvent` | Payment | Wallet | Debit owner wallet (reversal) |
| `WalletCreditedEvent` | Wallet | Notification | Notify owner of credit |
| `WithdrawalRequestedEvent` | Wallet | Payment | Audit withdrawal record |
| `WalletTransferRecordedEvent` | Wallet | Payment | Audit wallet-to-wallet transfer |
| `MaintenancePaymentConfirmedEvent` | Wallet (Phase 2) | Maintenance | Confirm ticket payment |

---

## Aggregate Boundaries

### WalletBalance (Aggregate Root)
- `Id`: WalletId (Guid)
- `OwnerId`: UserId (foreign identity — not a nav property)
- `AvailableBalance`: decimal
- `HeldBalance`: decimal (escrow — Phase 2)
- `Currency`: string (default "AED")
- `LedgerEntries`: collection of `LedgerEntry`
- **Invariant:** `AvailableBalance` cannot go below zero during Debit

### LedgerEntry (Entity inside WalletBalance aggregate)
- Append-only — never updated after creation
- Fields: Id, WalletId, Type (Credit/Debit/Hold/Release), Amount, Reference (PaymentId or WithdrawalId), CreatedAt, Description
- **Decision needed:** Keep as child entity of WalletBalance aggregate, OR as a separate read model table for performance. Recommended: child entity for writes, separate projection for reads. See open-questions.md Q13.

### WithdrawalRequest (Separate Aggregate Root)
- Separate lifecycle from WalletBalance
- Fields: Id, OwnerId, Amount, BankAccountDetails (value object), Status, RequestedAt, ProcessedAt, FailureReason
- Status: `Pending → Processing → Completed | Failed`
- WalletBalance.Debit() is called ONLY when WithdrawalRequest reaches `Processing`, not at request creation (to prevent over-debit before processing)
