# Estate Project Roadmap

> Missing endpoints, features, and fixes across **Estate-UI** (Angular) and the
> **Real-Estate-ITI-Graduation-Project** backend (.NET).
> Last re-audited 2026-06-13 (after browse-page redesign, company dashboard polish, and broker search).
> Owners per CLAUDE.md: **Ahmed** — Property/Subscription/Host wiring · **Mohamed** — AI · **Sara** — Identity/Admin.

Legend: ✅ done · 🔶 partially done · ⬜ open

---

## P0 — Correctness & security fixes (do first, all small)

| # | Status | Item | What to do | Where |
|---|---|---|---|---|
| 1 | ✅ | `/api/AI/chat` was anonymous and trusted `userId` from the body | Done by Mohamed: controller-level `[Authorize]`, user from JWT, routes moved to `/AI/chatbot/chat` (+ sessions/history) and `/AI/descriptions/{id}/accept`. All frontend services migrated to the new contract | `AIController` + frontend chat services |
| 2 | ✅ | `LeadsController` had **no `[Authorize]`** | Done: roles locked to broker/company_manager/admin; brokers can only read their own leads. (Per-lead company ownership check on UpdateStatus still worth adding with #15) | `LeadsController` |
| 3 | ✅ | AI quota never enforced — checker had **zero callers** | Done: chatbot handler and `PropertyAiDescriptionRequestHandler` now check + increment the monthly AI quota; controllers return 409 `{ error }` with the upgrade message | AI + Property modules |
| 4 | ✅ | `MaxInquiriesPerMonth` never checked | Done: `CreateInquiryCommand` checks + increments via new `CheckInquiryLimitAsync`/`IncrementInquiryUsageAsync`; controller returns 409 `{ error }` | Document + Subscription modules |
| 5 | ✅ | Tenant "Ask AI" tab sent the wrong payload (broken against the real backend) | Fixed: `tenant-dashboard/services/ai.service.ts` posts `{ sessionId?, message }` to `/AI/chatbot/chat` and tracks the session internally | Frontend |
| 6 | ✅ | Tenant dashboard (`/dashboard` catch-all) guarded by `authGuard` only — any role saw the tenant UI | Done: `roleGuard` + `expectedRole: 'tenant'` in `tenant-dashboard/dashboard.routes.ts` | Frontend routes |
| 7 | ✅ | Broker deactivate/delete never decremented `BrokersUsed`, so plan seats were consumed forever | Done via `SyncBrokerUsageAsync` — the counter realigns to the true active-broker count on every add/deactivate/reactivate/delete (self-heals historical drift) | Subscription + Identity modules |

## P1 — Missing backend endpoints

| # | Status | Endpoint | Purpose / notes |
|---|---|---|---|
| 8 | ✅ | `POST /api/auth/forgot-password` + `/reset-password` | Done — built on the existing OTP infra (6-digit emailed code, no DB migration). Frontend forgot-password page is a working 2-step flow |
| 9 | ✅ | `POST /api/subscriptions/mine/upgrade`, `/mine/cancel` | Done — new commands using the entity's `Upgrade()`/`Cancel()`; "Manage Plan" modal lists the real plan catalog and switches plans; Cancel has a confirm dialog. Charging stays stubbed until Payment ships |
| 10 | ✅ | `POST /api/companies/mine/logo` | Done — uploads via `IMediaStorageService` (5 MB cap, image types only), persists via new `UpdateCompanyLogoCommand`; Settings uploader is live with instant preview |
| 11 | ❌ | ~~`POST /api/AI/price-estimate`~~ | **Dropped by team decision (2026-06-12)** — removed from the subscription features UI and the company AI tab; the unused `HasAiPriceEstimation` DB column stays to avoid a migration |
| 12 | ✅ | Ratings summary | Done — `GET /api/companies/mine/ratings-summary` (new `IRatingStatsRepository`, matches by CompanyId or any company property, excludes rejected/deleted); Performance card now shows the real distribution with an empty state |
| 13 | ✅ | `GET /api/users` (admin, paged+search+role filter) + `PUT /{id}/ban` + `/unban` | Done — `AdminUserDto` includes ban info + last login; ready for Sara's Users page |
| 14 | ✅ | Storage tracking | Done — media upload checks `MaxStorageGb` before accepting (409 on overflow) and records `AddStorage(sizeGb)` after; the Storage usage meter now moves |

## P1 — Broker metrics pipeline (dashboard numbers stuck at 0)

| # | Status | Item | What to do |
|---|---|---|---|
| 15 | ✅ | `ClosedDealsCount` never increments | Done — closed-won leads publish `BrokerDealClosedEvent` (Shared.Kernel) with the assigned broker from `InquiryAssignments`; Identity handler increments the counter |
| 16 | ✅ | `AvgResponseTimeMinutes` never computed | Done — inquiry accept publishes `BrokerInquiryAcceptedEvent` with `AcceptedAt − CreatedAt`; Identity folds it into the broker's average |

## P2 — Missing frontend features

| # | Status | Feature | Notes |
|---|---|---|---|
| 17 | ✅ | **Public properties browse + detail pages** | Done — `/properties` browse grid (`GET /properties/active`) with a **left sticky filter sidebar**: multi-select type/city/bedrooms, min/max price, amenities dropdown (grouped by category), dismissible chips for every active filter, pagination (9 per page). `/properties/:id` detail page: gallery, specs, amenities list, listing broker/company, tenant **inquiry form** → `POST /api/inquiries`; non-tenants get a sign-in prompt. Navbar Properties link + home "View All / Explore" buttons wired; smart-search results deep-link to the detail page. Backend: `AmenityIds` added to `PropertySummaryDto` projection (additive, no migration) |
| 18 | ✅ | **Owner/broker "My Listings"** | Done — shared page at `/my-listings` (owners, navbar link) and `/dashboard/broker/listings` (brokers): all statuses, **Continue setup** reopens the wizard at step 2 fully prefilled (`?propertyId=`), Submit, Delete (frees the plan slot — `DeletePropertyHandler` now decrements usage), Mark as Rented. The wizard also shows a draft-recovery banner (Continue/Discard) on fresh opens |
| 19 | ⬜ | **Broker dashboard pages** | Listings (#18 done), Leads page on real `GET /leads/broker/{id}` + status updates; AI-score ordering once #16 lands |
| 20 | ⬜ | **Admin dashboard** | Pages are stubs. Backend ready: pending-verification queue, `SetVerification`, company verify, `/users/me` settings (shared `UserProfileService` in `core/services/`); needs #13. **Sara** |
| 21 | ⬜ | **Silent token refresh** | Backend `/auth/refresh` exists and the refresh token is stored. Interceptor: on 401 → refresh → retry once → only then `sessionExpired()` |
| 22 | ⬜ | **Navbar notifications bell** | Dead button; `NotificationsController` + tenant `NotificationService` already exist — make it role-aware |
| 23 | ⬜ | **Verify-OTP / email-verification wiring** | Backend `send-otp` / `verify-otp` exist; the pages are unwired stubs |

## P3 — Cleanup & consistency

| # | Status | Item |
|---|---|---|
| 24 | ⬜ | Delete Structure B dead code: `features/dashboards/{owner,broker,comp-manager,vendor}-dashboard`, `features/admin`, the orphaned `dashboards/tenant/` stub tree, remaining `rep.txt` files. NOTE: after the latest merge, `app.routes.ts` has BOTH a `/dashboard/tenant` route (old stub tree) and the `/dashboard` catch-all (real tenant dashboard) — remove the stub route when cleaning |
| 25 | ⬜ | Plan-gate featured listings (currently `AdminOnly` regardless of plan) — or accept it's admin-curated and remove it from the plan marketing |
| 26 | ⬜ | Tenant early-access / priority-response: define semantics (e.g. Active listings visible to premium tenants N hours early via a filter in `GetActiveProperties`) or drop from the seed |
| 27 | ⬜ | `environment.prod.ts` still points at a placeholder API URL |
| 28 | ⬜ | Bundle-budget warnings (initial bundle + `home.css`) |
| 29 | ⬜ | Prune dead `API_ENDPOINTS` entries; update the stale ITI `CLAUDE.md` (still says Identity is empty) |

---

## Suggested order of attack

- ~~**Week 1 (P0 + quick P1)**~~ ✅ **All P0 and all P1 are done** (except #11, dropped). Every subscription-plan promise is now either enforced server-side or visibly removed.
- ~~**Public properties pages (#17) + owner/broker listings (#18)**~~ ✅ **Done.**
- **Next:** broker dashboard leads page (#19) + admin dashboard (#20 — Sara). Silent token refresh (#21) unblocks the "session expired" UX.
- **Then:** notifications bell (#22), OTP page wiring (#23).
- **Last:** P3 cleanup before the final presentation.

---

## Design decisions (settled)

- **Downgrade overflow = grandfather.** Cancelling a premium plan drops to the matching free tier; properties/brokers already created above the new limit **stay active** — the limit only blocks *new* additions until usage drops back under it. A shared `<app-over-limit-banner>` surfaces the state on the company Overview/Properties/Brokers pages ("over your plan's X limit — N of M, new ones paused"). No data is ever deleted on downgrade.
- **Company brokers share the company pool.** A company broker's listings carry `CompanyId`, so they count against the company subscription (10 free / 200 premium), NOT a per-broker cap — one broker can fill the whole pool. Only *standalone* individual brokers have their own FreeIndividual 2-property limit. The 3-broker and 10-property free limits are independent company-level pools.

## Recently completed (for context)

- ✅ **Browse page redesign (2026-06-13):** left sticky filter sidebar, multi-select type/city/beds/amenities/price filters, dismissible chips, paginated 9-per-page grid. Backend: `AmenityIds` added to `PropertySummaryDto` projection
- ✅ **Company dashboard polish (2026-06-13):** Overview restructured — top stat-card row removed, Engagement section (Total views / Inquiries / This month / Deals closed) restored alongside the Property Status donut; Top Brokers and Recent Activity now side-by-side below. Settings page converted to tabbed layout (Company / Account / Password) matching the tenant Profile page
- ✅ **Broker search filter (2026-06-13):** Status tabs (All / Active / Inactive) + search input (name, email, license) added to the company Brokers page — same pattern as the Properties page
- ✅ Company dashboard fully live: `companies/mine/*` (details, brokers w/ names+metrics, stats, activity), `subscriptions/mine` (+usage, +payments)
- ✅ Auth: real login w/ role mapping, JWT expiry auto-logout + session-expired redirect, role-based landing (tenant → home, others → dashboard), CORS
- ✅ Property creation wizard (owner w/ ownership docs + AI check, broker without) → media/amenities/AI description (generate→edit→accept) → submit
- ✅ Broker lifecycle: deactivate, reactivate (limit re-check + self-healing seat counter), permanent delete (properties preserved) + assign-broker UI; real plan limits in page headers
- ✅ Company AI tab (premium-gated w/ blur + upgrade CTA) on the new `/AI/chatbot/chat` contract; manager account editor (`/users/me`); confirm modals
- ✅ By Mohamed: AIController secured + session-based chatbot (sessions/history endpoints)
- ✅ By Kandeel: public chatbot widget + AI smart search on the home page
