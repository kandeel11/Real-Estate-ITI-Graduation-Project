# Payment System — Endpoints & Commission Rules

## Overview

The platform charges a **1% commission** on two transaction types:
- Rent payments (charged to the landlord)
- Withdrawal requests (charged to the owner)

---

## Endpoints

### Rent Payments

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/payments/generate-monthly` | Generate pending rent payments for all active leases for the current month |
| `GET`  | `/api/payments` | List all rent payments (admin view) |
| `GET`  | `/api/payments/{id}` | Get a single payment by ID |
| `POST` | `/api/payments/{id}/process` | Manually process a pending payment (non-Stripe path) |
| `POST` | `/api/payments/webhook/stripe` | Stripe webhook — handles `payment_intent.succeeded`, `payment_intent.payment_failed`, `charge.refunded`, `checkout.session.completed`, `checkout.session.expired` |
| `GET`  | `/api/payments/overdue` | List overdue rent payments |
| `POST` | `/api/payments/{id}/late-fee` | Apply a late fee to an overdue payment |
| `GET`  | `/api/payments/{id}/receipt` | Get receipt URL for a paid payment |

### Wallet

| Method | Route | Description |
|--------|-------|-------------|
| `GET`  | `/api/wallet/balance` | Get the authenticated owner's wallet balance |
| `GET`  | `/api/wallet/ledger` | Get the paginated ledger (credit/debit history) |
| `POST` | `/api/wallet/withdraw` | Request a withdrawal (1% commission applied) |
| `GET`  | `/api/wallet/withdrawals` | List the owner's own withdrawal requests |
| `GET`  | `/api/wallet/withdrawals/all` | List all withdrawal requests (admin) |
| `POST` | `/api/wallet/withdrawals/{id}/approve` | Approve a withdrawal (admin) |
| `POST` | `/api/wallet/withdrawals/{id}/reject` | Reject a withdrawal (admin) |

### Lease Payment Schedule

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/leases/{id}/payment-schedule` | Returns installment periods, current/next period, due dates, and summary totals |

---

## Commission Rules

### 1. Rent Payment — 1% Platform Commission

When a rent payment succeeds (via Stripe webhook or manual `POST /api/payments/{id}/process`):

1. The `Payment.CommissionAmount` field is set to `Amount × 0.01`
2. A `RentPaymentSucceededEvent` is published to the Outbox
3. The `RentPaymentSucceededHandler` in the Wallet module credits the landlord's wallet with **`Amount × 0.99`** (net amount after commission)

**Example:**

| Installment Amount | Commission (1%) | Landlord Receives |
|--------------------|-----------------|-------------------|
| 5,000 AED          | 50 AED          | 4,950 AED         |
| 10,000 AED         | 100 AED         | 9,900 AED         |
| 60,000 AED         | 600 AED         | 59,400 AED        |

The platform retains the commission implicitly (not credited to a specific wallet — it stays in the platform's revenue).

The ledger entry description shows:
> "Rent payment for lease {leaseId} (net of 50.00 AED platform commission)"

### 2. Withdrawal Request — 1% Platform Commission

When an owner requests a withdrawal via `POST /api/wallet/withdraw`:

1. `WithdrawalRequest.CommissionAmount` = `RequestedAmount × 0.01`
2. `WithdrawalRequest.NetAmount` = `RequestedAmount × 0.99` (this is what the owner receives in their bank)
3. The **full requested amount** is debited from the owner's wallet balance immediately
4. A debit ledger entry is created recording the full deduction and showing the commission breakdown

**Example:**

| Requested Amount | Commission (1%) | Net Bank Payout | Wallet Debit |
|------------------|-----------------|-----------------|--------------|
| 10,000 AED       | 100 AED         | 9,900 AED       | 10,000 AED   |
| 50,000 AED       | 500 AED         | 49,500 AED      | 50,000 AED   |

The ledger entry description shows:
> "Withdrawal request — net payout 9,900.00 AED after 100.00 AED platform commission"

---

## Payment Flow Diagram

```
Tenant Pays Rent
      │
      ├─ Via Stripe Webhook ─────────────────────────────────┐
      │   payment_intent.succeeded                           │
      │                                                      │
      └─ Via Manual POST /api/payments/{id}/process ─────────┤
                                                             │
                                          ┌──────────────────▼────────────────────┐
                                          │  Payment.MarkAsPaid()                 │
                                          │  Payment.RecordCommission(0.01)       │
                                          │  → CommissionAmount = Amount × 0.01   │
                                          │  Outbox: RentPaymentSucceededEvent    │
                                          └──────────────────┬────────────────────┘
                                                             │ (async via Outbox)
                                          ┌──────────────────▼────────────────────┐
                                          │  RentPaymentSucceededHandler          │
                                          │  netAmount = Amount × 0.99            │
                                          │  Wallet.Credit(netAmount)             │
                                          │  LedgerEntry: Credit (net of 1%)      │
                                          └───────────────────────────────────────┘


Owner Requests Withdrawal
      │
      POST /api/wallet/withdraw
      │
      ┌──────────────────────────────────────────────────────┐
      │  RequestWithdrawalHandler                           │
      │  CommissionAmount = Amount × 0.01                   │
      │  NetAmount        = Amount × 0.99                   │
      │  Wallet.Debit(Amount)  ← full amount locked         │
      │  LedgerEntry: Debit (shows net + commission)        │
      │  WithdrawalRequest.Create(...)                      │
      └──────────────────────────────────────────────────────┘
                  │
                  │  (Admin approves later)
                  ▼
      POST /api/wallet/withdrawals/{id}/approve
      → Owner's bank receives NetAmount (99%)
```

---

## Database Changes

### `payment.payments` (new column)
| Column | Type | Default | Description |
|--------|------|---------|-------------|
| `CommissionAmount` | `decimal(12,2)` | `0` | 1% of Amount, set when payment is marked as paid |

### `wallet.wallet_withdrawal_requests` (new columns)
| Column | Type | Default | Description |
|--------|------|---------|-------------|
| `CommissionAmount` | `decimal(18,4)` | `0` | 1% of Amount |
| `NetAmount` | `decimal(18,4)` | `0` | Amount after commission (what the bank pays out) |

### Migrations Applied
- `Payment.Infrastructure`: `20260618200000_AddCommissionAmount`
- `Wallet.Infrastructure`: `20260618200001_AddWithdrawalCommission`

---

## Rent Payment Endpoint Verification

The rent payment flow works end-to-end:

1. **Generate payments** — `POST /api/payments/generate-monthly`
   - `GenerateMonthlyPaymentsHandler` queries all active leases via `ILeaseService.GetActiveLeasesAsync()`
   - Creates one `Payment` record per lease per month using `Payment.CreateRent(leaseId, propertyId, tenantId, landlordId, installmentAmount, dueDate)`
   - Uses `lease.InstallmentAmount` (renamed from `RentAmount` in the lease extraction phase)
   - Skips leases that already have a payment for the target month

2. **Process payment** — `POST /api/payments/{id}/process` with `{ "paymentMethod": "card", "cardToken": "tok_xxx" }`
   - Loads payment with `RentDetails` included (so `LandlordId` is available)
   - Sends to payment gateway
   - On success: marks paid, records 1% commission, publishes `RentPaymentSucceededEvent` to outbox
   - Outbox worker asynchronously credits landlord wallet with 99% of the amount

3. **Stripe webhook** — `POST /api/payments/webhook/stripe`
   - Same commission + outbox logic as the manual path
   - Handles Stripe's `payment_intent.succeeded` event
