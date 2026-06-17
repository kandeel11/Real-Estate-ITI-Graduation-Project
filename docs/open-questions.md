# Open Questions

Questions that must be answered before or during implementation. Grouped by blocking priority.

---

## Blocking — Must Answer Before Phase 1 Implementation

### Q1: Is there an escrow/hold period before crediting the owner?

**Context:** After Stripe confirms a rent payment, should the owner wallet be credited immediately, or should the funds be held for N days (e.g., to allow for refunds or disputes)?

**Why it blocks Phase 1:** If escrow is required, `WalletBalance` needs a `HeldBalance` field and a `Hold → Release` lifecycle. If not required, `AvailableBalance` is credited directly. These are different aggregate designs.

**Consequence of wrong choice:** Building without escrow and later adding it requires a migration + backfill of all existing ledger entries.

**Suggested default if no strong business reason:** No escrow in Phase 1. Credit immediately after `payment_intent.succeeded`. Add escrow in Phase 2 if chargebacks become a problem.

---

### Q2: What is the payment provider for rent payments?

**Context:** The codebase has `StripeWebhookService`, `CheckoutService`, and Stripe-specific configurations. But `IPaymentGateway` suggests an abstraction layer.

**Why it blocks Phase 1:** The event publish (`RentPaymentSucceededEvent`) happens inside `StripeWebhookService`. If rent payments can also be processed via a different provider (e.g., PayTabs, local gateway), that provider also needs to publish the same event.

**Current assumption:** Stripe only. Confirm this.

---

### Q3: Is OwnerId (LandlordId) guaranteed to be set on all existing RentPaymentDetails rows?

**Context:** `RentPaymentDetails.LandlordId` is the source of `OwnerId` for the wallet credit. If historical payments have `null` here, the event handler will receive null and may create an orphaned ledger entry or throw.

**Why it blocks Phase 1:** Need to know whether to add a null guard (skip event, log warning) or treat null as a fatal error (fail the webhook).

**Check this with:** `SELECT COUNT(*) FROM payment.rent_payments WHERE LandlordId IS NULL`

---

### Q4: Does the platform use a single currency (AED)?

**Context:** `WalletBalance` will have a `Currency` field. If all transactions are AED, this is a hardcoded constant. If multi-currency is needed, the ledger needs exchange rate snapshots.

**Why it blocks Phase 1:** Currency field type and balance comparison logic differ.

**Suggested default:** AED only. Enforce at wallet aggregate level. Reject anything else.

---

### Q5: Who is the authenticated user for `GET /wallet/balance`?

**Context:** JWT claims include `UserId`. A user may have the `Owner` role. The wallet is keyed by `OwnerId`. Are these the same value in practice?

**Why it blocks Phase 1:** The controller uses `User.GetId()` or equivalent. If `OwnerId` is the same as `UserId`, the query is straightforward. If `OwnerId` is a different domain concept (e.g., from a separate `Owners` table), the controller needs to resolve the mapping.

**Check this with:** How does `RentPaymentDetails.LandlordId` get set? Does it equal the User's JWT `sub` claim?

---

## Non-Blocking — Answer Before Phase 2

### Q6: What is the brokerage commission model?

**Context:** The wallet analysis identified this as a blocker for brokerage wallets. The platform must decide:
- Percentage per transaction (e.g., 2% of each rent payment)
- Fixed monthly fee per brokerage account
- Manual invoice / off-platform billing
- Hybrid (e.g., monthly fee + per-listing surcharge)

**Impact:** Determines whether brokerage wallet needs a `CommissionLedger`, a background job, or neither.

**Status:** Deferred from Phase 1. Must be answered before implementing brokerage wallet.

---

### Q7: How are vendor payments handled in Phase 2?

**Context:** `MaintenancePayment` inside the Maintenance module is an architectural smell. Phase 2 should migrate this to Wallet → Payment flow. But vendor payments require vendors to have a bank account or wallet on the platform.

**Questions:**
- Are vendors registered users of the platform?
- Do they receive payouts via the platform, or are they paid externally and just tracked here?
- Is there a vendor acceptance/KYC process?

**Impact:** If vendors are paid externally, Phase 2 just needs to record the payment intent without a payout API. If vendors receive platform payouts, Stripe Connect is required.

---

### Q8: What happens if a refund arrives after the owner has already withdrawn the credited funds?

**Context:** Documented in wallet-analysis.md. A `charge.refunded` Stripe event requires debiting the owner wallet. If the owner already withdrew the money, the balance may be insufficient.

**Options:**
- Allow negative balance (debt) — owner owes the platform
- Block refund from debiting wallet if balance < refund amount — platform absorbs the loss
- Escrow hold period prevents this — refund window closes before owner can withdraw

**Impact:** Changes the `WalletBalance.Debit()` invariant. Phase 1 defers refund handling entirely (no `RentPaymentRefundedEvent` handler in Phase 1).

---

### Q9: What payout mechanism is used for owner withdrawals?

**Context:** Phase 1 stores `WithdrawalRequest` with status `Pending` and does nothing else. Phase 2 must process actual payouts.

**Options:**
- Stripe Connect (instant/standard payouts to connected accounts)
- Local bank transfer API (UAE-specific — e.g., CBUAE API, UAEFTS)
- Manual admin approval → external bank transfer → mark processed
- PayPal or other third-party payout service

**Impact:** Determines whether `BankAccountDetails` value object needs IBAN validation specific to UAE format, and whether an admin approval workflow is needed.

---

### Q10: Should withdrawals require admin approval or be automatic?

**Context:** Phase 1 records `WithdrawalRequest.Status = Pending`. Phase 2 processes it. Who decides it moves to `Processing`?

**Options:**
- Automatic after N hours (if balance confirmed)
- Admin approval via dashboard before any payout
- Automatic below a threshold (e.g., < 10,000 AED), manual above

**Impact:** Determines whether an admin API endpoint (`PATCH /admin/withdrawals/{id}/approve`) is needed in Phase 2.

---

### Q11: Is wallet keyed by UserId only, or by UserId + Role?

**Context:** Raised in wallet-analysis.md. A user with both Owner and Brokerage roles conceptually has two different wallet relationships. If one person is both an Owner (receiving rent) and a registered Broker (paying commission), should they have one wallet or two?

**Suggested default:** One wallet per user (keyed by UserId only). Different ledger entry descriptions distinguish credit sources. Multiple wallets add complexity with no clear business benefit unless there is a legal/regulatory reason to separate funds.

**Impact:** Changes the WalletBalance aggregate key and the lookup logic.

---

### Q12: Does the Notification module need to be integrated in Phase 1?

**Context:** Phase 1 plan deferred `WalletCreditedEvent` → email notification. The Notification module exists and is integrated for payment confirmations.

**Question:** Is it acceptable for Phase 1 to credit the wallet silently (no email), or must the owner receive a "your wallet was credited" notification on go-live?

**Impact:** If required in Phase 1, `WalletCreditedEvent` must be added to SharedKernel and Notification module must subscribe. Low effort, but not zero.

---

### Q13: LedgerEntry — aggregate child or separate read model?

**Context:** `LedgerEntry` is currently designed as a child entity inside the `WalletBalance` aggregate. As ledger history grows (hundreds of entries per owner per year), loading the full aggregate will become slow.

**Options:**
- Keep as aggregate child (simple, consistent, lazy-load)
- Separate read model table (`wallet.ledger_entries` projected from `WalletBalance` events, queried directly without loading aggregate)
- Hybrid: child for writes, separate query using `DbContext.LedgerEntries.Where(e => e.WalletId == id)` for reads without loading the whole aggregate

**Suggested default:** Hybrid. WalletBalance aggregate owns LedgerEntry creation. `GetLedgerHandler` queries the `ledger_entries` table directly via `WalletDbContext` without loading the full aggregate.

**Impact:** Changes whether `GetLedgerHandler` loads `WalletBalance` or queries `LedgerEntry` directly. Low risk to decide late.

---

## Technical Debt Tracking

These are not questions but decisions already made that need to be revisited:

| Item | Phase | Current State | Target State |
|---|---|---|---|
| `MaintenancePayment` in Maintenance module | 2 | Parallel payment system bypassing Payment module | Migrate to Wallet → Payment flow |
| `PaymentMethod.Wallet` enum with no handler | 1 | Placeholder only | Filled by `WalletTransferRecordedEvent` handler in Payment module |
| Brokerage commission model | 2 | Undefined | Implement after commission model is decided |
| Refund reversal against wallet | 2 | No handler for `charge.refunded` → wallet | Add `RentPaymentRefundedEvent` and handler |
| Withdrawal payout processing | 2 | `WithdrawalRequest.Status = Pending` forever | Actual payout via chosen mechanism |
| Vendor payout | 2 | External / off-platform | Platform-mediated if vendors are registered users |
