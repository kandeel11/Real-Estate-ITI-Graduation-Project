# Maintenance Company — Validation & Password Management

## Architecture Overview

The maintenance company feature is split across **two separate modules**, each with its own DbContext and schema.

```
Identity module (schema: identity)
  └─ identity.users                    ← credentials for all roles (email, passwordHash, role)
  └─ identity.maintenance_companies    ← lightweight identity-side reference (currently stub)

MaintenanceCompanies module (schema: maintenanceComp)
  └─ maintenanceComp.MaintenanceCompanies  ← full operational profile (categories, hours, reviews)
```

### Create Flow (Admin Only)

```
POST /api/maintenance-companies
  │
  ├─ Step 1: RegisterMaintenanceCompanyCommand → Identity module
  │     • Validates email format (FluentValidation)
  │     • Checks identity.users for duplicate email
  │     • Creates User(role=maintenance_company), auto-verifies email
  │     • Returns UserId (Guid)
  │
  └─ Step 2: CreateMaintenanceCompanyCommand → MaintenanceCompanies module
        • Checks maintenanceComp.MaintenanceCompanies for duplicate email
        • Validates CategoryIds are active
        • Creates company profile + seeds 7 default working hours
        • Returns MaintenanceCompanyResponseDto
```

**Email is checked at two layers:**
1. `identity.users` — prevents duplicate login accounts
2. `maintenanceComp.MaintenanceCompanies` — prevents duplicate company profiles

---

## What Was Implemented

### 1. FluentValidation for `RegisterMaintenanceCompanyCommand`

File: `Identity.Application/Commands/RegisterMaintenanceCompany/RegisterMaintenanceCompanyCommandValidator.cs`

| Field | Rules |
|---|---|
| `FullName` | NotEmpty, MaxLength(200) |
| `Email` | NotEmpty, valid email format, MaxLength(256) |
| `Phone` | NotEmpty, MaxLength(20) |
| `TempPassword` | NotEmpty, MinimumLength(8), MaxLength(100) |

The validator is auto-discovered by the `AddValidatorsFromAssembly(...)` call already present in `IdentityModuleExtensions`. MediatR's FluentValidation pipeline behavior fires **before the handler**, so invalid requests never reach the database.

### 2. Email Uniqueness Check in Company Profile Handler

File: `MaintenanceCompanies.Application/Commands/CreateMaintenanceCompanyCommandHandler.cs`

```csharp
var emailNormalized = request.Email.Trim().ToLowerInvariant();
if (await _db.MaintenanceCompanies.AnyAsync(c => c.Email == emailNormalized, cancellationToken))
    throw new InvalidOperationException("A maintenance company with this email already exists.");
```

Returns HTTP 400 `{"error": "A maintenance company with this email already exists."}` instead of an unhandled `DbUpdateException` from the unique index.

### 3. `ChangePasswordHandler` — Reflection Bug Fixed

File: `Identity.Application/Commands/ChangePassword/ChangePasswordCommand.cs`

**Before (broken):**
```csharp
var prop = user.GetType().GetProperty("PasswordHash");
prop?.SetValue(user, hasher.Hash(cmd.NewPassword));
user.MarkAsUpdated();
```

**After (correct):**
```csharp
user.SetPasswordHash(hasher.Hash(cmd.NewPassword));
```

`User.SetPasswordHash()` sets the hash and calls `MarkAsUpdated()` internally. Reflection bypassed the domain model and would silently do nothing if the property name ever changed.

### 4. Dedicated Password Endpoint for Maintenance Company

File: `Host/Controllers/MaintenanceCompaniesController.cs`

```
PATCH /api/maintenance-companies/password
Authorization: Bearer <maintenance_company_jwt>

Body: { "currentPassword": "...", "newPassword": "..." }

200 OK: { "message": "Password updated successfully." }
400 Bad Request: { "error": "Current password is incorrect." }
```

Requires role `maintenance_company` (policy `MaintenanceCompany`). Internally dispatches the existing `ChangePasswordCommand` using the JWT `UserId` claim.

---

## Error Response Reference

| Scenario | HTTP | Body |
|---|---|---|
| Invalid email format in request | 400 | FluentValidation error list |
| Email already registered (identity.users) | 400 | `{"error": "Email is already registered."}` |
| Email already used by a company profile | 400 | `{"error": "A maintenance company with this email already exists."}` |
| Category ID not found or inactive | 400 | `{"error": "One or more categories are invalid or inactive."}` |
| Wrong current password on change | 400 | `{"error": "Current password is incorrect."}` |

---

## What Is NOT in Scope (Deferred)

- **`identity.maintenance_companies` table**: exists in `IdentityDbContext` but nothing writes to it. It is a stub. Populating it (or removing it) is a separate architectural decision.
- **Password reset via OTP**: `User.SetPasswordResetOtp()` + `VerifyPasswordResetOtp()` already exist on the domain entity. A full forgot-password flow for maintenance companies is deferred.
- **New password strength policy** beyond minimum length: can be added to the validator when policy is decided.
