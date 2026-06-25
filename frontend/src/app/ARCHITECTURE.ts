/**
 * REAL ESTATE PLATFORM - ANGULAR 22 ARCHITECTURE
 * =====================================================
 *
 * Domain-Driven Design Implementation with Standalone Components & Signals
 *
 * DIRECTORY STRUCTURE (src/app)
 * =====================================================
 *
 * src/
 * │
 * ├── app/
 * │   │
 * │   ├── core/                           [Core Module - Singletons & Global Services]
 * │   │   ├── guards/
 * │   │   │   └── role.guard.ts          [Functional role-based access control guard]
 * │   │   ├── interceptors/
 * │   │   │   └── auth.interceptor.ts    [HTTP interceptor for Authorization header]
 * │   │   ├── services/
 * │   │   │   └── auth.service.ts        [Auth service with Signal-based state]
 * │   │   └── constants/
 * │   │       └── api-endpoints.ts       [Centralized API endpoint configuration]
 * │   │
 * │   ├── shared/                        [Reusable Components & Utilities]
 * │   │   ├── components/
 * │   │   │   ├── button/
 * │   │   │   ├── modal/
 * │   │   │   ├── table/
 * │   │   │   ├── card/
 * │   │   │   └── pagination/
 * │   │   ├── directives/
 * │   │   │   ├── debounce.directive.ts
 * │   │   │   ├── has-role.directive.ts
 * │   │   │   └── loading.directive.ts
 * │   │   └── pipes/
 * │   │       ├── currency.pipe.ts
 * │   │       ├── phone-format.pipe.ts
 * │   │       └── address-format.pipe.ts
 * │   │
 * │   ├── layouts/                       [Layout Shells - Route Wrappers]
 * │   │   ├── public-layout/
 * │   │   │   └── public-layout.component.ts        [Navigation + Footer]
 * │   │   ├── auth-layout/
 * │   │   │   └── auth-layout.component.ts          [Centered form layout]
 * │   │   └── dashboard-layout/
 * │   │       └── dashboard-layout.component.ts     [Sidebar + header navigation]
 * │   │
 * │   ├── features/                      [Feature Modules - Business Logic]
 * │   │   ├── auth/
 * │   │   │   ├── login/
 * │   │   │   │   └── login.component.ts
 * │   │   │   ├── register/
 * │   │   │   │   └── register.component.ts
 * │   │   │   ├── verify-email/
 * │   │   │   │   └── verify-email.component.ts
 * │   │   │   ├── verify-otp/
 * │   │   │   │   └── verify-otp.component.ts
 * │   │   │   └── forgot-password/
 * │   │   │       └── forgot-password.component.ts
 * │   │   │
 * │   │   ├── properties/
 * │   │   │   ├── property-list/
 * │   │   │   │   └── property-list.component.ts
 * │   │   │   ├── property-search/
 * │   │   │   │   └── property-search.component.ts
 * │   │   │   └── property-detail/
 * │   │   │       └── property-detail.component.ts
 * │   │   │
 * │   │   ├── ai/
 * │   │   │   └── chat-rag/
 * │   │   │       └── chat-rag.component.ts      [AI Chat with RAG]
 * │   │   │
 * │   │   ├── maintenance/
 * │   │   │   └── [maintenance feature components]
 * │   │   │
 * │   │   ├── subscriptions/
 * │   │   │   └── [subscription feature components]
 * │   │   │
 * │   │   └── payments/
 * │   │       └── [payment feature components]
 * │   │
 * │   └── dashboards/                    [Role-Specific Dashboards]
 * │       ├── admin/
 * │       │   ├── admin-dashboard.component.ts
 * │       │   └── pages/
 * │       │       ├── overview/
 * │       │       ├── users-management/
 * │       │       ├── companies-management/
 * │       │       ├── moderation/
 * │       │       └── reports/
 * │       │
 * │       ├── company/
 * │       │   ├── company-dashboard.component.ts
 * │       │   └── pages/
 * │       │       ├── overview/
 * │       │       ├── properties/
 * │       │       ├── brokers/
 * │       │       ├── leases/
 * │       │       └── subscriptions/
 * │       │
 * │       ├── broker/
 * │       │   ├── broker-dashboard.component.ts
 * │       │   └── pages/
 * │       │       ├── overview/
 * │       │       ├── listings/
 * │       │       ├── performance/
 * │       │       └── commissions/
 * │       │
 * │       ├── tenant/
 * │       │   ├── tenant-dashboard.component.ts
 * │       │   └── pages/
 * │       │       ├── overview/
 * │       │       ├── my-property/
 * │       │       ├── lease/
 * │       │       ├── maintenance-requests/
 * │       │       └── payments/
 * │       │
 * │       └── maintenance-company/
 * │           ├── maintenance-company-dashboard.component.ts
 * │           └── pages/
 * │               ├── overview/
 * │               ├── tickets/
 * │               ├── work-orders/
 * │               └── reports/
 * │
 * ├── app.routes.ts                     [Global routing configuration]
 * ├── app.config.ts                     [Application configuration & providers]
 * └── app.component.ts                  [Root component]
 *
 * =====================================================
 * ARCHITECTURAL PRINCIPLES
 * =====================================================
 *
 * 1. STANDALONE COMPONENTS
 *    - All components are standalone (no NgModule)
 *    - Improved tree-shaking and bundle optimization
 *    - Simplified dependency management
 *
 * 2. SIGNAL-BASED STATE MANAGEMENT
 *    - Reactive signals for reactive state
 *    - Computed signals for derived state
 *    - Fine-grained reactivity without RxJS dependency
 *
 * 3. LAZY LOADING
 *    - Layouts are lazy-loaded at route level
 *    - Features are lazy-loaded per route
 *    - Dashboard sub-routes can further optimize with lazy-loading
 *    - PreloadAllModules strategy for optimal performance
 *
 * 4. FUNCTIONAL GUARDS
 *    - roleGuard: Single role validation
 *    - multiRoleGuard: Multiple role validation
 *    - authGuard: Authentication check only
 *    - Clean, composable, reusable
 *
 * 5. DOMAIN-DRIVEN DESIGN
 *    - Features folder mirrors backend domain structure
 *    - Each feature is self-contained and independently deployable
 *    - Clear separation of concerns
 *    - 5 isolated dashboard contexts with specific role access
 *
 * 6. SEPARATION OF CONCERNS
 *    - core/: Singletons, global services, guards, constants
 *    - shared/: Reusable UI components, directives, pipes
 *    - layouts/: Route shells
 *    - features/: Feature-specific logic
 *    - dashboards/: Role-specific UI contexts
 *
 * =====================================================
 * ROUTING STRATEGY
 * =====================================================
 *
 * ROOT LEVEL:
 *   /                   → Public Layout (Home, Search, Property Details, AI Chat)
 *   /auth/*             → Auth Layout (Login, Register, Verify, OTP, Forgot Password)
 *   /dashboard/*        → Dashboard Layout + Role-Based Child Routes
 *
 * DASHBOARD ROUTES:
 *   /dashboard/admin/           → Admin Dashboard (only role='admin')
 *   /dashboard/company/         → Company Dashboard (only role='company')
 *   /dashboard/broker/          → Broker Dashboard (only role='broker')
 *   /dashboard/tenant/          → Tenant Dashboard (only role='tenant')
 *   /dashboard/maintenance-company/ → Maintenance Dashboard (only role='maintenance-company')
 *
 * GUARD MECHANISM:
 *   Each dashboard route is protected by roleGuard with expectedRole in route data
 *   Unauthorized users are redirected to /auth/login with returnUrl
 *   Auth state is managed via Angular Signals for reactivity
 *
 * =====================================================
 * API ENDPOINT STRUCTURE
 * =====================================================
 *
 * Auth Module:        /auth/login, /auth/register, /auth/verify-email, etc.
 * Properties:         /properties, /properties/:id, /properties/search, etc.
 * Companies:          /companies, /companies/:id, /companies/:id/properties, etc.
 * Brokers:            /brokers, /brokers/:id, /brokers/:id/listings, etc.
 * Maintenance:        /maintenance/tickets, /maintenance/tickets/:id, etc.
 * Leases:             /leases, /leases/:id, /leases/:id/renew, etc.
 * Subscriptions:      /subscriptions/plans, /subscriptions/user, etc.
 * Payments:           /payments/create-intent, /payments/confirm, etc.
 * Smart Search:       /smart-search/search, /smart-search/advanced, etc.
 * Chat & RAG:         /chat-rag/message, /chat-rag/conversation, etc.
 *
 * =====================================================
 * KEY FEATURES
 * =====================================================
 *
 * ✓ Strictly Standalone Components
 * ✓ Angular Signals for reactive state
 * ✓ Functional Route Guards with role-based access
 * ✓ Optimized lazy loading at layout and feature levels
 * ✓ PreloadAllModules strategy for performance
 * ✓ Clean, type-safe API endpoint configuration
 * ✓ 5 role-specific dashboards with isolated navigation
 * ✓ Centralized HTTP interceptor for authentication
 * ✓ Production-ready code with inline comments
 * ✓ DDD alignment with backend ASP.NET Core Monolith
 *
 * =====================================================
 */

export const ARCHITECTURE_NOTES = {
  version: '1.0',
  angularVersion: '22.x',
  typescript: '5.x',
  buildTool: 'Angular CLI',
  stateManagement: 'Angular Signals',
  componentStrategy: 'Standalone Components Only',
  routingStrategy: 'Feature-based with Lazy Loading',
  httpClient: 'Angular HttpClient',
};
