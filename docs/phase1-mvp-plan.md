# Phase 1 MVP Plan — Owner Wallet

## Scope Statement

Phase 1 delivers a functional owner wallet with three capabilities: automatic credit on rent payment, balance query, and withdrawal request recording. No actual payout processing. No brokerage, vendor, or tenant wallets. No commission logic.

**Done means:** An owner can log in, see their wallet balance, see every credit entry with its source payment, and submit a withdrawal request. The withdrawal request is stored with status `Pending` — no money moves in Phase 1.

---

## Out of Scope (Phase 1)

These items are explicitly NOT in Phase 1 scope. Do not implement them even if they seem easy:

- Actual withdrawal payout (Stripe Connect, bank transfer, manual release)
- Brokerage wallet or commission deduction
- Vendor wallet or maintenance payment via wallet
- Hold/escrow period before crediting owner
- Tenant wallet
- Wallet-to-wallet transfer between owners
- Refund reversal against wallet (debit on refund webhook)
- Admin wallet management UI
- Balance notifications (email "your wallet was credited") — deferred until Notification module integration is confirmed

---

## What Gets Built

### New: Wallet Module

Directory structure under `src/Modules/Wallet/`:

```
Wallet.Domain/
  Aggregates/
    WalletBalance.cs          ← aggregate root
    WithdrawalRequest.cs      ← separate aggregate root
  Entities/
    LedgerEntry.cs
  Enums/
    LedgerEntryType.cs        ← Credit, Debit
    WithdrawalStatus.cs       ← Pending, Processing, Completed, Failed
  ValueObjects/
    BankAccountDetails.cs
  Interfaces/
    IWalletRepository.cs
    IWithdrawalRepository.cs

Wallet.Application/
  Commands/
    CreditWallet/
      CreditWalletCommand.cs
      CreditWalletHandler.cs
    RequestWithdrawal/
      RequestWithdrawalCommand.cs
      RequestWithdrawalHandler.cs
      RequestWithdrawalCommandValidator.cs
  Queries/
    GetBalance/
      GetBalanceQuery.cs
      GetBalanceHandler.cs
      WalletBalanceDto.cs
    GetLedger/
      GetLedgerQuery.cs
      GetLedgerHandler.cs
      LedgerEntryDto.cs
  EventHandlers/
    RentPaymentSucceededEventHandler.cs

Wallet.Infrastructure/
  Persistence/
    WalletDbContext.cs
    Repositories/
      WalletRepository.cs
      WithdrawalRepository.cs
    Migrations/
      (EF migrations)
  DependencyInjection.cs

Wallet.API/
  Controllers/
    WalletController.cs       ← GET /wallet/balance, GET /wallet/ledger, POST /wallet/withdrawals
```

### New: SharedKernel Integration Event

File: `src/Shared/RealEstate.Shared.Kernel/Shared/IntegrationEvents/RentPaymentSucceededEvent.cs`

Modeled identically to `SubscriptionPaymentSucceededEvent`. Carries: `PaymentId`, `OwnerId` (= `RentPaymentDetails.LandlordId`), `TenantId`, `LeaseId`, `Amount`.

### Modified: Payment Module

**File:** `StripeWebhookService.cs` — add outbox publish of `RentPaymentSucceededEvent` inside `HandlePaymentSucceededAsync()` when `payment.PaymentType == PaymentType.Rent`.

No other changes to Payment module domain or schema.

---

## Implementation Sequence

### Step 1 — SharedKernel: Add RentPaymentSucceededEvent

**Files:**
- Create `RentPaymentSucceededEvent.cs` in SharedKernel

**Why first:** Every subsequent step depends on this type existing. Unblocks Payment module change and Wallet event handler simultaneously.

**Acceptance:** File compiles. Record carries PaymentId, OwnerId, TenantId, LeaseId, Amount, PaidAt.

---

### Step 2 — Payment Module: Publish Event

**Files:**
- `StripeWebhookService.cs` — add `_outboxWriter.AddAsync(new RentPaymentSucceededEvent(...))` after `payment.MarkAsPaid()` where `PaymentType == Rent`

**Constraint:** Must be inside the same transaction as `payment.MarkAsPaid()`. If outbox write fails, rent payment also rolls back (atomicity guaranteed by existing UoW pattern).

**Where OwnerId comes from:** `payment.RentDetails.LandlordId`

**Acceptance:** When Stripe sends `payment_intent.succeeded` for a rent payment, `outbox_messages` table gets a row with `EventType = "RentPaymentSucceededEvent"`.

---

### Step 3 — Wallet Domain

**Files:**
- `WalletBalance.cs`: `Credit(decimal amount, Guid referenceId, string description)` → creates `LedgerEntry`, updates `AvailableBalance`. Throws if amount ≤ 0.
- `WithdrawalRequest.cs`: `Create(Guid ownerId, decimal amount, BankAccountDetails bank)` → validates amount ≤ available balance (checked at application layer, not here — aggregate trusts the caller), status = Pending.
- `LedgerEntry.cs`: properties only (Id, WalletId, Type, Amount, Reference, Description, CreatedAt). No behavior.
- `BankAccountDetails.cs`: value object (AccountNumber, BankName, IBAN, AccountHolderName).

**Acceptance:** Domain layer compiles with no infrastructure dependencies.

---

### Step 4 — Wallet Infrastructure

**Files:**
- `WalletDbContext.cs`: registers `WalletBalance`, `LedgerEntry`, `WithdrawalRequest`. Schema `wallet`. Table names: `wallet_balances`, `ledger_entries`, `withdrawal_requests`.
- `WalletRepository.cs`: `GetByOwnerIdAsync(Guid ownerId)`, `CreateAsync(WalletBalance wallet)`, `SaveAsync(WalletBalance wallet)`.
- `WithdrawalRepository.cs`: `CreateAsync(WithdrawalRequest request)`, `GetByOwnerIdAsync(Guid ownerId)`.
- EF migration: initial schema for 3 tables.
- `DependencyInjection.cs`: registers DbContext, repositories, MediatR handlers, event handler.

**Idempotency constraint:** `LedgerEntry` must have a unique index on `(WalletId, Reference)` to prevent double-credit if `RentPaymentSucceededEvent` is delivered twice.

**Acceptance:** Migration runs. Tables exist in `wallet` schema.

---

### Step 5 — Wallet Application Layer

**Files:**
- `RentPaymentSucceededEventHandler.cs`: implements `INotificationHandler<RentPaymentSucceededEvent>`. Loads or creates `WalletBalance` for `OwnerId`. Calls `wallet.Credit(event.Amount, event.PaymentId, "Rent payment - Lease {event.LeaseId}")`. Saves.
- `CreditWalletHandler.cs`: internal command handler (called by event handler, not directly by API).
- `RequestWithdrawalHandler.cs`: validates `amount > 0` AND `amount <= wallet.AvailableBalance`. Creates `WithdrawalRequest`. Saves.
- `GetBalanceHandler.cs`: returns `WalletBalanceDto { OwnerId, AvailableBalance, Currency, LastUpdatedAt }`.
- `GetLedgerHandler.cs`: returns paged list of `LedgerEntryDto { Id, Type, Amount, Reference, Description, CreatedAt }` ordered by `CreatedAt DESC`.

**Acceptance:** Unit tests for `RequestWithdrawalHandler` pass: (a) amount > balance → throws, (b) valid amount → WithdrawalRequest created with status Pending.

---

### Step 6 — Wallet API Controller

**File:** `WalletController.cs`

Endpoints:
```
GET  /api/wallet/balance          → GetBalanceQuery(currentUserId)    [Authorize]
GET  /api/wallet/ledger           → GetLedgerQuery(currentUserId, pageNumber, pageSize) [Authorize]
POST /api/wallet/withdrawals      → RequestWithdrawalCommand(currentUserId, amount, bankDetails) [Authorize]
```

No admin endpoints in Phase 1. Owner sees only their own wallet (enforced by using `currentUserId` from JWT claims, not from request body).

**Acceptance:** All 3 endpoints return 200 with correct data when called by authenticated owner.

---

### Step 7 — Host Registration

**File:** `Program.cs`
- Register `WalletDbContext` with connection string
- Call `Wallet.Infrastructure.DependencyInjection.AddWalletModule(builder.Services, ...)`
- Ensure `RentPaymentSucceededEvent` handler is picked up by MediatR scan

**Acceptance:** App starts. No startup errors. `/api/wallet/balance` returns 401 for unauthenticated request (not 404, not 500).

---

## Milestones

| Milestone | Steps Included | Done When |
|---|---|---|
| M1: Event pipe connected | Steps 1-2 | Outbox row appears after simulated rent payment |
| M2: Wallet domain + DB | Steps 3-4 | Migration runs, tables exist |
| M3: Credit flow end-to-end | Steps 1-5 | Rent payment → wallet credited in DB |
| M4: API live | Steps 6-7 | Owner can query balance via Postman |

---

## Risk Mitigation

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| RentPaymentSucceededEvent delivered twice (outbox retry) | Medium | Double credit | Unique constraint on `(WalletId, Reference)` — second insert throws, handler catches and no-ops |
| StripeWebhookService change breaks existing rent payment flow | High | Payments stop | Add event publish AFTER `payment.MarkAsPaid()`, not inside it. Roll back only if outbox write fails, which is acceptable. |
| WalletDbContext connection string misconfigured | Low | App startup fails | Same pattern as `PaymentDbContext` — check `Program.cs` and verify at startup |
| Owner submits withdrawal for more than available balance | Medium | Invalid state | Application layer check in `RequestWithdrawalHandler` before creating aggregate |
| `RentPaymentDetails.LandlordId` is null | Low | NullRef in event handler | Add null guard in `StripeWebhookService` — skip outbox publish and log error if LandlordId is missing |

---

## Definition of Done (Phase 1)

- [ ] `RentPaymentSucceededEvent` exists in SharedKernel
- [ ] Payment module publishes event after confirmed rent payment
- [ ] Wallet module schema exists in database (`wallet` schema, 3 tables)
- [ ] `WalletBalance` is created automatically for owner on first credit
- [ ] Balance query returns correct amount after at least one credit
- [ ] Ledger query returns entries with correct amounts and references
- [ ] Withdrawal request can be submitted and is stored with status `Pending`
- [ ] Withdrawal request is rejected if amount > available balance
- [ ] No changes to existing Stripe webhook processing behavior
- [ ] No changes to existing Payment module schema
