# Architecture Decision Record — Wallet Module Placement

**Date:** 2026-06-18
**Status:** Proposed
**Deciders:** Platform Architecture Team
**Context:** The platform needs a wallet system to track owner balances after rent collection. The existing Payment module handles Stripe payments but has no balance/ledger capability.

---

## Problem Statement

After Stripe confirms a rent payment, where does the money tracking logic live? Three options exist. The wrong choice will either create a god module, introduce leaky boundaries, or add unnecessary overhead.

---

## Option A — Wallet Inside Payment Module

**Description:** Add `WalletBalance`, `LedgerEntry`, and `WithdrawalRequest` entities directly to `Payment.Domain`. The Payment module becomes responsible for Stripe orchestration AND internal balance tracking.

**What this looks like:**
```
Payment.Domain/
  Entities/
    Payment.cs           (existing)
    WalletBalance.cs     (new)
    LedgerEntry.cs       (new)
    WithdrawalRequest.cs (new)
```

**Advantages:**
- No new module scaffolding required
- Single DbContext for all financial data (simpler joins)
- One place to look for all money-related code
- Fastest to start

**Disadvantages:**
- Payment module already has: `Payment`, `RentPaymentDetails`, `SubscriptionPaymentDetails`, `TransferPaymentDetails`, `OutboxMessage`, Stripe services, background jobs. Adding balance tracking makes it a god module.
- Wallet lifecycle is completely different from payment lifecycle. A `WalletBalance` is long-lived and mutable. A `Payment` is append-only and immutable after confirmation. Mixing them violates aggregate design principles.
- Any bug in wallet logic can break Stripe webhook processing and vice versa — same deploy, same context.
- Testing becomes harder: every test setup requires Stripe mocks even for simple balance queries.
- Commission logic, withdrawal states, and transfer tracking all have different domain rules that have nothing to do with Stripe.

**Coupling:** High. Adding brokerage wallets, vendor wallets, or escrow logic in Phase 2 will require modifying the Payment module, which already handles production Stripe webhooks.

**Verdict:** Rejected.

---

## Option B — Separate Wallet Module (Pure Bounded Context)

**Description:** A new `Wallet` module with its own domain, application layer, DbContext, and API. No shared code with Payment module except integration events. Payment module publishes `RentPaymentSucceededEvent` → Wallet module subscribes and credits balance.

**What this looks like:**
```
Wallet.Domain/
  Aggregates/
    WalletBalance.cs
    WithdrawalRequest.cs
  ValueObjects/
    LedgerEntry.cs
    Money.cs

Wallet.Application/
  Commands/
    CreditWallet/
    RequestWithdrawal/
  Queries/
    GetBalance/
    GetLedger/
  EventHandlers/
    RentPaymentSucceededEventHandler.cs
```

**Advantages:**
- Clean bounded context — Wallet domain knows nothing about Stripe
- Independent deploy units (important if moving to microservices later)
- Wallet domain model can evolve without touching Payment module
- Single responsibility: Wallet = balance tracking only
- Easy to test in isolation

**Disadvantages:**
- New module scaffolding: Domain, Application, Infrastructure, migrations, DI registration
- Cross-module consistency requires outbox + eventual consistency (not immediate)
- Two DbContexts = no cross-module transactions (must handle via saga/process manager for complex flows)
- Higher initial effort

**Coupling:** Low. This is the textbook DDD approach.

**Verdict:** Correct approach in principle, but assessed against codebase evidence below.

---

## Option C — Hybrid (RECOMMENDED)

**Description:** Same as Option B (separate Wallet module with its own bounded context), but acknowledges that the existing Payment module already contains partial wallet intent that must be preserved for backward compatibility.

**What the codebase tells us:**

The existing `Payment` entity already has:
- `PaymentType.Deposit` — conceptually a wallet top-up
- `PaymentType.Withdrawal` — conceptually a wallet payout
- `PaymentType.Transfer` — conceptually a wallet-to-wallet transfer
- `TransferPaymentDetails` (SenderId, ReceiverId, Purpose) — clearly a wallet transfer record
- `PaymentMethod.Wallet` — a payment method enum value with zero implementation

This is not coincidence. Someone designed the Payment entity to be the "single source of truth for all financial transactions" (comment in `Payment.cs`). The intent was always to unify everything here — but that creates the god module problem.

**The hybrid resolution:**
1. Payment module retains `PaymentType.Transfer/Deposit/Withdrawal` for audit trail purposes — every Wallet operation that involves external money movement creates a corresponding `Payment` record.
2. The **Wallet module owns the business logic**: balance state, invariants, withdrawal lifecycle.
3. For internal transfers (Owner → Vendor): Wallet module orchestrates, Payment module records the transaction.
4. `TransferPaymentDetails.SenderId/ReceiverId` maps exactly to wallet transfer participants — Wallet module publishes a `WalletTransferRecordedEvent` → Payment module creates a `Payment` audit record.

**Boundary contract:**
```
Stripe webhook confirmed
  → Payment module: payment.MarkAsPaid() + publish RentPaymentSucceededEvent (via outbox)
  → Wallet module: subscribes to event → WalletBalance.Credit()

Owner requests withdrawal
  → Wallet module: WithdrawalRequest.Create() → publish WithdrawalRequestedEvent
  → Payment module: subscribes → Payment.CreateWithdrawal() (audit record)

Owner pays vendor
  → Wallet module: WalletBalance.Debit() → publish WalletTransferRecordedEvent
  → Payment module: subscribes → Payment.CreateTransfer() (audit record)
```

**Advantages:**
- Clean wallet domain (identical to Option B)
- Payment module retains its audit/ledger role — all external money movement is still recorded in `payment.payments` table
- Backward compatible — existing `PaymentType` enum values get their implementation
- `OutboxWriter` and `OutboxPublisherJob` are already implemented — reuse directly
- Pattern already established: `SubscriptionPaymentSucceededEvent` shows exact implementation template for `RentPaymentSucceededEvent`

**Disadvantages:**
- Two modules must coordinate for some flows (eventual consistency, not transactional)
- Slightly more integration event surface area

**Coupling:** Low between modules. High cohesion within each module.

---

## Decision

**Recommended: Option C — Hybrid**

**Justification tied directly to codebase:**

1. `PaymentMethod.Wallet` exists with no implementation. This is a placeholder that proves the original design intended wallet support — it needs to be filled by a proper Wallet module, not bolted onto the Payment module.

2. `SubscriptionPaymentSucceededEvent` in SharedKernel proves the team already established the pattern of Payment publishing events → other modules reacting. `RentPaymentSucceededEvent` follows the identical pattern.

3. `TransferPaymentDetails` already models wallet transfers at the data level. The Wallet module will add the business rules; Payment module retains the record.

4. The `OutboxWriter` + `OutboxPublisherJob` infrastructure is production-ready. No new infrastructure needed for cross-module communication.

5. Option A would turn `PaymentDbContext` into a financial database that mixes Stripe webhook processing with balance queries — a testability and maintenance nightmare.

---

## Consequences

- A new `Wallet` module must be created under `src/Modules/Wallet/`
- `RentPaymentSucceededEvent` must be added to `RealEstate.Shared.Kernel`
- `StripeWebhookService.HandlePaymentSucceededAsync` must publish `RentPaymentSucceededEvent` via outbox when `PaymentType == Rent`
- `PaymentMethod.Wallet` enum value gets a real meaning: used when Payment records a wallet-sourced transaction
- No existing code is deleted or broken
