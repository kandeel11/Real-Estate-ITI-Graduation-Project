# Platform Commission & Wallet System

The platform automatically deducts a fee from every financial transaction. All fees accumulate as the platform's revenue and are visible in the admin dashboard.

---

## Fee Structure

| Transaction type | Rate | Who pays | Who receives |
|---|---|---|---|
| Rent payment | **1%** | Tenant (via Stripe) | Landlord wallet credited with 99% |
| Maintenance payment | **2%** | Tenant / Owner (via Stripe Checkout) | Vendor receives 98% (tracked) |
| Wallet withdrawal | **1%** | Owner (deducted from withdrawal) | Owner bank receives 99% |

---

## Rent Payments (1% commission)

### Flow
1. Monthly job generates a `Payment` record (status `Pending`) for each active lease installment.
2. Frontend calls `POST /api/payments/{id}/initiate` → receives `clientSecret` + `publishableKey`.
3. Frontend renders Stripe Elements, calls `stripe.confirmCardPayment(clientSecret)`.
4. Stripe fires `payment_intent.succeeded` webhook → backend calls `payment.RecordCommission(0.01m)` → sets `CommissionAmount = Amount × 0.01`.
5. `RentPaymentSucceededEvent` published via outbox.
6. `RentPaymentSucceededHandler` credits the landlord's wallet with **99%** of the rent amount.

### Ledger entry description
```
Rent payment for lease {leaseId} (net of {commission} AED platform commission)
```

### Key files
- `Payment.Domain/Entities/Payment.cs` — `RecordCommission(decimal rate = 0.01m)`
- `Payment.Infrastructure/Services/Stripe/StripeWebhookService.cs` — calls `RecordCommission()` in `HandlePaymentSucceededAsync`
- `Wallet.Application/EventHandlers/RentPaymentSucceededHandler.cs` — credits wallet

---

## Maintenance Payments (2% commission)

### Flow
1. Maintenance company creates a payment record via `POST /api/maintenance/tickets/{id}/payments`.
2. Payer calls `POST /api/maintenance/tickets/{id}/payments/checkout` → receives Stripe Checkout URL.
3. Payer is redirected to the Stripe-hosted page.
4. Stripe fires `checkout.session.completed` webhook → `HandleMaintenancePaymentConfirmedHandler` calls `payment.TryConfirmViaWebhook()`.
5. `TryConfirmViaWebhook` internally calls `RecordCommission(0.02m)`:
   - `CommissionAmount = Amount × 0.02`
   - `NetAmount = Amount × 0.98`
6. Ticket is marked `Completed`.

### Key files
- `Maintenance.Domain/Entities/MaintenancePayment.cs` — `RecordCommission(decimal rate = 0.02m)`, called inside `TryConfirmViaWebhook`
- `Maintenance.Application/Features/EventHandlers/HandleMaintenancePaymentConfirmedHandler.cs`

---

## Wallet Withdrawals (1% commission)

### Flow
1. Owner requests a withdrawal via `POST /api/wallet/withdrawals`.
2. Handler calls `WithdrawalRequest.Create(ownerId, amount, bankDetails, commissionRate: 0.01m)`:
   - `CommissionAmount = Amount × 0.01`
   - `NetAmount = Amount - CommissionAmount`
3. Full `Amount` is debited from the wallet balance.
4. Admin approves via `PUT /api/wallet/admin/withdrawals/{id}/approve`.
5. Bank transfer is for `NetAmount` (handled externally).

### Ledger entry description
```
Withdrawal request — net payout {NetAmount} AED after {CommissionAmount} AED platform commission
```

### Key files
- `Wallet.Domain/Entities/WithdrawalRequest.cs` — `Create()` factory
- `Wallet.Application/Commands/RequestWithdrawal/RequestWithdrawalHandler.cs`

---

## Admin Dashboard Endpoints

### `GET /api/payments/summary`
Basic rent payment summary (admin only).

```json
{
  "totalCollected": 150000.00,
  "totalPending": 25000.00,
  "totalLateFees": 3000.00,
  "totalOverduePayments": 5,
  "totalRentCommission": 1500.00
}
```

### `GET /api/payments/platform-dashboard`
Full cross-module commission breakdown (admin only).

```json
{
  "rent": {
    "totalCollected": 150000.00,
    "totalCommission": 1500.00,
    "commissionRate": "1%",
    "totalPending": 25000.00,
    "overdueCount": 5,
    "totalLateFees": 3000.00
  },
  "maintenance": {
    "totalCollected": 50000.00,
    "totalCommission": 1000.00,
    "commissionRate": "2%",
    "totalNetToVendors": 49000.00,
    "paidPaymentsCount": 12
  },
  "withdrawals": {
    "totalWithdrawn": 80000.00,
    "totalCommission": 800.00,
    "commissionRate": "1%",
    "totalNetPaidOut": 79200.00,
    "pendingCount": 3,
    "pendingAmount": 15000.00,
    "completedCount": 8
  },
  "platform": {
    "totalCommissionEarned": 3300.00,
    "breakdown": {
      "fromRent": 1500.00,
      "fromMaintenance": 1000.00,
      "fromWithdrawals": 800.00
    }
  }
}
```

---

## Wallet Endpoints

| Method | Endpoint | Role | Description |
|---|---|---|---|
| `GET` | `/api/wallet/balance` | owner, broker, company_manager, admin | Current wallet balance |
| `GET` | `/api/wallet/ledger` | owner, broker, company_manager, admin | Paginated transaction history |
| `POST` | `/api/wallet/withdrawals` | owner, broker, company_manager | Request a withdrawal |
| `GET` | `/api/wallet/withdrawals` | owner, broker, company_manager | Own withdrawal history |
| `GET` | `/api/wallet/admin/withdrawals` | admin | All withdrawal requests |
| `PUT` | `/api/wallet/admin/withdrawals/{id}/approve` | admin | Approve a withdrawal |
| `PUT` | `/api/wallet/admin/withdrawals/{id}/reject` | admin | Reject a withdrawal |

### Withdrawal request body
```json
{
  "amount": 5000.00,
  "bankAccountDetails": "IBAN: SA12 3456 7890 1234 5678 90"
}
```

---

## Database Schema

### `payment.payments`
| Column | Type | Notes |
|---|---|---|
| `CommissionAmount` | `decimal(12,2)` | 1% of Amount, set on confirmation |

### `maintenance.maintenance_payments`
| Column | Type | Notes |
|---|---|---|
| `CommissionAmount` | `decimal(10,2)` | 2% of Amount, set when Stripe confirms |
| `NetAmount` | `decimal(10,2)` | 98% of Amount |

### `wallet.wallet_withdrawal_requests`
| Column | Type | Notes |
|---|---|---|
| `CommissionAmount` | `decimal(18,4)` | 1% of Amount |
| `NetAmount` | `decimal(18,4)` | 99% of Amount |

### `wallet.wallet_balances`
Tracks each owner's current spendable balance (after 1% rent deduction).

### `wallet.wallet_ledger_entries`
Audit log of every credit and debit with commission details in `Description`.

---

## Applying Migrations

Run after deploying this version to create the new columns:

```bash
# Maintenance commission columns
dotnet ef database update --project src/Modules/Maintenance/Maintenance.Infrastructure \
  --startup-project src/Host/RealEstate.Host \
  --migration AddCommissionToMaintenancePayments

# Wallet commission columns (already applied in previous sprint)
dotnet ef database update --project src/Modules/Wallet/Wallet.Infrastructure \
  --startup-project src/Host/RealEstate.Host
```
