# Swagger Testing Guide — Lease Upload → Rent Payment Flow

Complete step-by-step walkthrough to test the full rent payment cycle in Swagger UI.  
Open Swagger at: **`https://localhost:{port}/swagger`**

---

## Prerequisites — Accounts You Need

You need **three accounts** set up before starting:

| Role | Purpose |
|------|---------|
| `broker` | Uploads lease PDF, sends offer to tenant |
| `tenant` | Accepts the lease offer |
| `owner` (landlord) | Receives wallet credit after payment |

If they don't exist yet, register each one first (Step 0).

---

## Step 0 — Register & Login

### 0.1 Register Broker

**`POST /api/auth/register`**

```json
{
  "fullName": "Ahmed Broker",
  "email": "broker@test.com",
  "password": "Test@1234",
  "phone": "01012345678",
  "role": "broker",
  "nationalIdNumber": "12345678901234"
}
```

### 0.2 Register Tenant

**`POST /api/auth/register`**

```json
{
  "fullName": "Mohamed Tenant",
  "email": "tenant@test.com",
  "password": "Test@1234",
  "phone": "01098765432",
  "role": "tenant",
  "nationalIdNumber": "43210987654321"
}
```

### 0.3 Register Owner (Landlord)

**`POST /api/auth/register`**

```json
{
  "fullName": "Khalid Owner",
  "email": "owner@test.com",
  "password": "Test@1234",
  "phone": "01055512345",
  "role": "owner",
  "nationalIdNumber": "98765432109876"
}
```

### 0.4 Login as Broker

**`POST /api/auth/login`**

```json
{
  "email": "broker@test.com",
  "password": "Test@1234"
}
```

**Response:**
```json
{
  "accessToken": "eyJhbGci...",
  "refreshToken": "...",
  "userId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
}
```

> **Copy the `accessToken`.**  
> In Swagger, click **Authorize** (top right) → paste `Bearer eyJhbGci...` → click Authorize.

---

## Step 1 — Upload Lease Contract (Broker)

> **Authenticated as: Broker**

**`POST /api/leases/upload`** (multipart/form-data)

| Field | Value |
|-------|-------|
| `leadId` | GUID of the lead/property this lease is for (get from your leads list) |
| `file` | A PDF file of the lease contract |

**In Swagger:**  
1. Click on `POST /api/leases/upload`
2. Click **Try it out**
3. Enter a valid `leadId` (GUID)
4. Click **Choose File** and select a lease PDF
5. Click **Execute**

**Expected Response (200):**
```json
{
  "leaseId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  "status": "pending",
  "message": "Lease uploaded. AI extraction in progress."
}
```

> **Copy the `leaseId`** — you'll use it in every subsequent step.

---

## Step 2 — Review AI Extraction

> **Authenticated as: Broker**

Wait a few seconds for the AI extraction to complete, then:

**`GET /api/leases/{leaseId}/ai-extraction`**

**Expected Response (200):**
```json
{
  "tenantName": "Mohamed Tenant",
  "tenantEmail": "tenant@test.com",
  "propertyTitle": "Apartment in Cairo",
  "startDate": "2026-07-01",
  "endDate": "2027-06-30",
  "installmentAmount": 5000.00,
  "paymentFrequency": "Monthly",
  "totalRentAmount": 60000.00,
  "durationInMonths": 12,
  "deposit": 10000.00,
  "extractionStatus": "done",
  "confidence": 0.92
}
```

> If `extractionStatus` is `"pending"`, wait a few more seconds and retry.  
> If it's `"failed"`, re-upload the PDF with clearer text.

---

## Step 3 — Send Lease to Tenant (Broker Confirms Terms)

> **Authenticated as: Broker**

**`POST /api/leases/{leaseId}/send-to-tenant`**

```json
{
  "installmentAmount": 5000.00,
  "deposit": 10000.00,
  "startDate": "2026-07-01",
  "endDate": "2027-06-30"
}
```

> You can override any AI-extracted value here. Leave a field null to keep the AI value.

**Expected Response: `204 No Content`**

---

## Step 4 — Tenant Accepts the Lease

> **Switch user: Login as Tenant**

**`POST /api/auth/login`** with tenant credentials, then re-Authorize in Swagger.

### 4.1 Check the Pending Offer

**`GET /api/tenant/leases/pending-offer`**

**Expected Response (200):**
```json
{
  "id": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  "propertyTitle": "Apartment in Cairo",
  "startDate": "2026-07-01",
  "endDate": "2027-06-30",
  "installmentAmount": 5000.00,
  "paymentFrequency": "Monthly",
  "totalRentAmount": 60000.00,
  "deposit": 10000.00,
  "pdfUrl": "https://...",
  "status": "pending_tenant"
}
```

### 4.2 Accept the Offer

**`POST /api/tenant/leases/{leaseId}/accept`**

No request body needed.

**Expected Response (200):**
```json
{
  "leaseId": "bbbbbbbb-...",
  "status": "active"
}
```

---

## Step 5 — View Payment Schedule

> **Can be tested as Broker or Tenant**

**`GET /api/leases/{leaseId}/payment-schedule`**

**Expected Response (200):**
```json
{
  "leaseId": "bbbbbbbb-...",
  "installmentAmount": 5000.00,
  "paymentFrequency": "Monthly",
  "totalRentAmount": 60000.00,
  "durationInMonths": 12,
  "startDate": "2026-07-01",
  "endDate": "2027-06-30",
  "paymentDayOfMonth": 1,
  "currentPeriod": {
    "periodNumber": 1,
    "periodStart": "2026-07-01",
    "periodEnd": "2026-07-31",
    "dueDate": "2026-07-01",
    "amount": 5000.00,
    "isCurrent": true,
    "isNext": false,
    "isPast": false
  },
  "nextPeriod": {
    "periodNumber": 2,
    "periodStart": "2026-08-01",
    "periodEnd": "2026-08-31",
    "dueDate": "2026-08-01",
    "amount": 5000.00,
    "isCurrent": false,
    "isNext": true,
    "isPast": false
  },
  "allPeriods": [ ... 12 periods ... ],
  "summary": {
    "totalInstallments": 12,
    "pastInstallments": 0,
    "currentInstallments": 1,
    "futureInstallments": 11,
    "totalAmount": 60000.00,
    "amountDueSoFar": 5000.00,
    "amountRemaining": 55000.00
  }
}
```

> **Error cases:**  
> `404` — lease not found  
> `422` — lease extraction is incomplete (missing installmentAmount / frequency / duration)

---

## Step 6 — Payments Are Auto-Generated (No Action Needed)

When the tenant accepts the lease (Step 4.2), **all installment payment records are created automatically**.

For a 12-month monthly lease starting 2026-07-01, you will immediately have 12 `Pending` payment records (one per month) without any manual step.

> **Optional — generate-monthly (manual fallback):**  
> `POST /api/payments/generate-monthly?targetMonth=2026-07-01`  
> Only needed if you want to manually trigger generation for a specific month (e.g. for leases activated before this feature was deployed).

---

## Step 7 — Find the Generated Payments

**`GET /api/payments?leaseId={leaseId}`**

**Expected Response (200):**
```json
[
  {
    "id": "cccccccc-cccc-cccc-cccc-cccccccccccc",
    "amount": 5000.00,
    "commissionAmount": 0.00,
    "status": "Pending",
    "dueDate": "2026-07-01",
    "paymentType": "Rent",
    "leaseId": "bbbbbbbb-...",
    "tenantId": "...",
    "landlordId": "..."
  }
]
```

> **Copy the payment `id`** (`cccccccc-...`) for the next step.

---

## Step 8 — Process the Payment ✅ 1% Commission Applied Here

**`POST /api/payments/{paymentId}/process`**

```json
{
  "paymentMethod": "cash",
  "cardToken": null
}
```

> Valid `paymentMethod` values: `"cash"`, `"card"`, `"banktransfer"`, `"wallet"`, `"stripe"`  
> `cardToken` is only needed for card/Stripe payments.

**Expected Response (200):**
```json
{
  "isSuccess": true,
  "transactionId": "TXN-MOCK-1234567890",
  "receiptUrl": null,
  "errorMessage": null
}
```

**What happens internally:**
1. Payment status → `Paid`
2. `CommissionAmount` = `5000 × 1%` = **50 AED** (stored on the Payment record)
3. `RentPaymentSucceededEvent` is queued in the Outbox
4. Outbox worker fires → landlord wallet credited with **4,950 AED** (99% of 5,000)

**Verify commission was recorded:**

**`GET /api/payments/{paymentId}`**

```json
{
  "id": "cccccccc-...",
  "amount": 5000.00,
  "commissionAmount": 50.00,
  "status": "Paid",
  "paidAt": "2026-06-18T20:00:00Z"
}
```

---

## Step 9 — Verify Landlord Wallet Was Credited

> **Switch user: Login as Owner (Landlord)**

### 9.1 Check Balance

**`GET /api/wallet/balance`**

```json
{
  "ownerId": "dddddddd-...",
  "balance": 4950.00,
  "currency": "AED"
}
```

> Balance is **4,950 AED** (not 5,000) — the 50 AED commission was retained by the platform.

### 9.2 Check Ledger Entry

**`GET /api/wallet/ledger`**

```json
{
  "items": [
    {
      "type": "Credit",
      "amount": 4950.00,
      "description": "Rent payment for lease bbbbbbbb-... (net of 50.00 AED platform commission)",
      "occurredAt": "2026-06-18T20:00:05Z"
    }
  ],
  "total": 1
}
```

---

## Step 10 — Owner Requests a Withdrawal ✅ 1% Commission Applied Here

> **Authenticated as: Owner**

**`POST /api/wallet/withdrawals`**

```json
{
  "amount": 4950.00,
  "bankAccountDetails": "IBAN: AE070331234567890123456"
}
```

**Expected Response (201):**
```json
{
  "withdrawalId": "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"
}
```

**What happens internally:**
- `CommissionAmount` = `4,950 × 1%` = **49.50 AED**
- `NetAmount` = `4,950 - 49.50` = **4,900.50 AED** ← this is what the bank pays out
- Wallet is debited **4,950 AED** immediately
- A `Debit` ledger entry is created

**Verify the balance dropped:**

**`GET /api/wallet/balance`**

```json
{
  "balance": 0.00,
  "currency": "AED"
}
```

**Check the debit ledger entry:**

**`GET /api/wallet/ledger`**

```json
{
  "items": [
    {
      "type": "Debit",
      "amount": 4950.00,
      "description": "Withdrawal request — net payout 4900.50 AED after 49.50 AED platform commission",
      "occurredAt": "2026-06-18T20:01:00Z"
    },
    {
      "type": "Credit",
      "amount": 4950.00,
      "description": "Rent payment for lease bbbbbbbb-... (net of 50.00 AED platform commission)"
    }
  ]
}
```

---

## Step 11 — Admin Approves the Withdrawal

> **Switch user: Login as Admin**

### 11.1 List All Pending Withdrawals

**`GET /api/wallet/admin/withdrawals`**

```json
[
  {
    "id": "eeeeeeee-...",
    "ownerId": "dddddddd-...",
    "amount": 4950.00,
    "commissionAmount": 49.50,
    "netAmount": 4900.50,
    "status": "Pending",
    "bankAccountDetails": "IBAN: AE070331234567890123456"
  }
]
```

### 11.2 Approve

**`PUT /api/wallet/admin/withdrawals/{withdrawalId}/approve`**

No body needed.

**Expected Response: `204 No Content`**

> The withdrawal status changes to `Completed`. The owner's bank receives **4,900.50 AED**.

---

## Commission Summary

| Transaction | Gross Amount | 1% Commission | Net Received |
|-------------|-------------|---------------|--------------|
| Rent Payment | 5,000 AED | 50 AED | Landlord gets 4,950 AED in wallet |
| Withdrawal   | 4,950 AED  | 49.50 AED | Bank receives 4,900.50 AED |

---

## Quick Error Reference

| Status | Meaning | Fix |
|--------|---------|-----|
| `401 Unauthorized` | Missing or expired JWT | Re-login, re-Authorize in Swagger |
| `403 Forbidden` | Wrong role | Switch to the correct user account |
| `404` on lease | `leaseId` wrong or deleted | Re-copy from Step 1 response |
| `422` on payment-schedule | Extraction incomplete | Check `/ai-extraction` status first |
| `400 Insufficient wallet balance` | Balance < withdrawal amount | Withdraw only what's in the wallet |
| `400 Payment already processed` | Payment is already Paid | Fetch fresh payment — check status |
| `400 from /generate-monthly` | No active leases found | Tenant must accept the lease (Step 4) first |

---

## Full Flow at a Glance

```
[Broker] POST /api/leases/upload                    → leaseId
[Broker] GET  /api/leases/{id}/ai-extraction        → review extracted terms
[Broker] POST /api/leases/{id}/send-to-tenant       → sends offer email to tenant

[Tenant] GET  /api/tenant/leases/pending-offer      → see the offer
[Tenant] POST /api/tenant/leases/{id}/accept        → lease is now Active
                                                      ↓ LeaseActivatedPaymentHandler fires
                                                      ↓ ALL installment records created automatically

[Any]    GET  /api/leases/{id}/payment-schedule     → full installment schedule
[Any]    GET  /api/payments?leaseId={id}            → see all auto-generated payments

[Admin]  POST /api/payments/{id}/process            → marks Paid + 1% commission + outbox event

(async)  OutboxPublisherJob fires RentPaymentSucceededEvent
(async)  RentPaymentSucceededHandler credits landlord wallet with Amount × 99%

[Owner]  GET  /api/wallet/balance                   → see 4,950 AED
[Owner]  GET  /api/wallet/ledger                    → see credit entry with commission note
[Owner]  POST /api/wallet/withdrawals               → request payout (1% commission deducted)
[Admin]  PUT  /api/wallet/admin/withdrawals/{id}/approve → bank payout approved
```
