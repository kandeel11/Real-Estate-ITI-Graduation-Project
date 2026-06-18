# Wallet Analysis

## Business Purpose

The wallet is an internal ledger that tracks money owed to platform actors. After a tenant pays rent through Stripe, the platform must record that the owner is owed that money and eventually facilitate a payout. Without a wallet, there is no mechanism to track balances, hold funds pending settlement, distribute commissions, or process withdrawals. The wallet is not a payment processor — Stripe does that. The wallet is the internal accounting layer that answers "how much does each actor have on the platform?"

---

## Who Owns Wallets — Challenged Assumptions

| Actor | Phase 1 Wallet? | Rationale |
|---|---|---|
| **Owner (Landlord)** | **Yes — required** | Receives rent after Stripe confirms payment. Must be able to see balance, request withdrawal. Without this, the platform cannot track what's owed to the owner. |
| **Tenant** | **No — correct** | Tenant pays directly via Stripe. No platform balance needed. This assumption holds. |
| **Brokerage Company** | **Defer — not Phase 1** | Commission model is undefined. Building a brokerage wallet without knowing if commission is per-transaction, monthly flat fee, or manual invoice is premature. Defer to Phase 2. |
| **Vendor / Maintenance Provider** | **No — correct for Phase 1** | Vendor currently gets paid outside the platform or via `MaintenancePayment` (a separate smell — addressed in module-boundaries.md). Wallet integration requires vendor onboarding. Defer. |
| **Platform Admin** | **No business wallet — correct** | Admin needs a financial visibility dashboard (read model only), not a spendable balance. |

**Challenge on Owner assumption:** The assumption says "Owner wallet required" — this is correct but incomplete. An Owner can also be a Brokerage member or even a Tenant on another lease. The wallet must be keyed by **UserId + Role**, not just UserId, OR scoped to the Owner role only. See open-questions.md Q11.

---

## Core Wallet Operations

| Operation | Description | Trigger |
|---|---|---|
| **Credit** | Add funds to balance | Rent payment confirmed by Stripe |
| **Debit** | Reduce balance | Owner pays vendor via wallet, commission deducted |
| **Hold (Escrow)** | Reserve funds, not yet available | Optional: hold rent N days before crediting owner |
| **Release** | Convert held amount to available balance | Escrow period ends |
| **Withdraw** | Owner requests payout to bank account | Owner-initiated |

---

## Money Flow Scenarios

### 1. Rent Payment → Owner Wallet Credit

```
Tenant (browser)
  → Stripe Checkout (CreateSubscriptionPayment or direct PaymentIntent)
  → Stripe confirms payment
  → Stripe sends webhook to platform
  → StripeWebhookService.HandlePaymentSucceededAsync()
      → payment.MarkAsPaid()
      → OutboxWriter.AddAsync(RentPaymentSucceededEvent {PaymentId, OwnerId (= LandlordId from RentPaymentDetails), Amount, LeaseId})
  → OutboxPublisherJob dispatches RentPaymentSucceededEvent
  → Wallet module: WalletCreditHandler
      → Load or create WalletBalance for OwnerId
      → Create LedgerEntry (Credit, Amount, Reference = PaymentId)
      → WalletBalance.Credit(amount)
      → Save
```

**Key note:** `OwnerId` is available as `RentPaymentDetails.LandlordId`. The `RentPaymentSucceededEvent` must carry this. It does NOT exist yet in SharedKernel — must be added (similar to `SubscriptionPaymentSucceededEvent`).

---

### 2. Brokerage Commission

**Two models under consideration (business decision required):**

**Option A — Percentage per transaction (e.g., 2% of rent)**
```
RentPaymentSucceededEvent received by Wallet module
  → Calculate commission = Amount × rate
  → Debit owner wallet by commission amount
  → Credit brokerage wallet by commission amount
  → Create two LedgerEntries: one debit (owner), one credit (brokerage)
```

**Option B — Fixed monthly fee (brokerage pays platform, not per-transaction)**
```
Monthly background job
  → For each active brokerage with active listings
  → Debit brokerage wallet by flat fee
  → Credit platform ledger (or just record as fee collected)
```

**Status: BLOCKED.** Commission model is undefined. Do not implement brokerage wallet in Phase 1.

---

### 3. Owner Withdrawal

```
Owner (API)
  → POST /wallet/withdrawals {Amount, BankAccountDetails}
  → WithdrawalRequest.Create(ownerId, amount, bankDetails)
  → Status = Pending
  → Save to DB
  → (Phase 1 stops here — request is recorded, not processed)

Phase 2:
  → Admin reviews OR automated job
  → Trigger payout via Stripe Connect / local bank API
  → WithdrawalRequest.MarkProcessed(transactionRef)
  → WalletBalance.Debit(amount)
  → LedgerEntry(Debit, Amount, Reference = WithdrawalId)
```

**Status: Partially in Phase 1 (record request only). Payout processing deferred.**

---

### 4. Owner Pays Maintenance Vendor via Wallet

```
MaintenanceTicket reaches AwaitingPayment status
  → Owner approves payment to vendor
  → Wallet module: WalletTransferCommand(fromOwnerId, toVendorId, amount, ticketId)
  → WalletBalance(owner).Debit(amount)
  → LedgerEntry(Debit, owner, Reference = TicketId)
  → (Vendor wallet does not exist in Phase 1 — payout happens outside platform)
```

**Status: DEFERRED to Phase 2.** Vendor wallet not in scope. Current `MaintenancePayment` entity in Maintenance module is a smell — it bypasses the Wallet entirely. This must be migrated in Phase 2.

---

### 5. Refund / Reversal

```
Stripe sends charge.refunded webhook
  → StripeWebhookService.HandleChargeRefundedAsync()
      → payment.MarkAsRefunded()
      → OutboxWriter.AddAsync(RentPaymentRefundedEvent {PaymentId, OwnerId, Amount})
  → Wallet module: WalletDebitHandler
      → If owner wallet was already credited for this payment
      → WalletBalance.Debit(amount)  [cannot go negative — enforce invariant]
      → LedgerEntry(Debit, Amount, Reference = OriginalPaymentId, Type = Reversal)
```

**Risk:** If owner already withdrew the credited amount, wallet balance may be insufficient for reversal. A hold/escrow period mitigates this. See open-questions.md Q8.

---

## Codebase Mapping to Wallet Concepts

| Existing Code | Maps to Wallet Concept | Status |
|---|---|---|
| `Payment.CreateTransfer(senderId, receiverId, ...)` | Internal wallet-to-wallet transfer | Intent exists, no implementation |
| `Payment.CreateDeposit(amount)` | Wallet top-up / fund injection | Intent exists, no implementation |
| `Payment.CreateWithdrawal(amount)` | Withdrawal request | Intent exists, no implementation |
| `PaymentMethod.Wallet` | Internal wallet payment method | Enum value only, no handler |
| `TransferPaymentDetails` (SenderId, ReceiverId) | Wallet transfer metadata | Exists in Payment domain |
| `OutboxWriter` + `OutboxPublisherJob` | Event delivery to Wallet module | Fully implemented — reuse |
| `SubscriptionPaymentSucceededEvent` | Pattern for `RentPaymentSucceededEvent` | Must be created in SharedKernel |

---

## Risks and Complexity

| Risk | Severity | Mitigation |
|---|---|---|
| Double-credit on retry | High | Idempotency key on LedgerEntry using PaymentId — unique constraint prevents duplicate entries |
| Withdrawal of unconfirmed funds | High | Only credit wallet after Stripe `payment_intent.succeeded` — never on `Pending` |
| Refund after withdrawal (balance negative) | High | Escrow hold period (business decision) OR minimum reserve requirement |
| Maintenance module bypasses Wallet | Medium | Documented smell, deferred to Phase 2 migration |
| OwnerId not on Payment entity | Low | Available via `RentPaymentDetails.LandlordId` — carry in integration event |
| Currency mismatch | Low | Platform uses AED only — enforce at wallet aggregate level |
