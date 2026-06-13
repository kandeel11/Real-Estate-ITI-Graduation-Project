import { Routes } from '@angular/router';
import { roleGuard, multiRoleGuard, dashboardRedirectGuard, guestGuard } from './core/guards/role.guard';

export const routes: Routes = [
  // ==================== PUBLIC LAYOUT (Navbar + Footer) ====================
  {
    path: '',
    loadComponent: () =>
      import('./layout/main-layout/main-layout.component').then((m) => m.MainLayoutComponent),
    children: [
      {
        path: '',
        canActivate: [dashboardRedirectGuard],
        loadComponent: () => import('./features/home/home').then((m) => m.HomeComponent),
      },
      {
        path: 'properties/new',
        canActivate: [multiRoleGuard],
        data: { expectedRoles: ['owner', 'broker'] },
        loadComponent: () =>
          import('./features/properties/pages/create-property/create-property.component').then(
            (m) => m.CreatePropertyComponent,
          ),
      },
      {
        path: 'my-listings',
        canActivate: [multiRoleGuard],
        data: { expectedRoles: ['owner', 'broker'] },
        loadComponent: () =>
          import('./features/properties/pages/my-listings/my-listings.component').then(
            (m) => m.MyListingsComponent,
          ),
      },
      {
        path: 'properties',
        loadComponent: () =>
          import('./features/properties/pages/browse/browse.component').then(
            (m) => m.BrowseComponent,
          ),
      },
      {
        path: 'properties/:id',
        loadComponent: () =>
          import('./features/properties/pages/detail/detail.component').then(
            (m) => m.DetailComponent,
          ),
      },
    ],
  },

  // ==================== AUTH ROUTES ====================
  {
    path: 'auth',
    children: [
      {
        path: 'login',
        canActivate: [guestGuard],
        loadComponent: () =>
          import('./features/auth/pages/login/login.component').then((m) => m.LoginComponent),
      },
      {
        path: 'register',
        canActivate: [guestGuard],
        loadComponent: () =>
          import('./features/auth/pages/register/register.component').then(
            (m) => m.RegisterComponent,
          ),
      },
      {
        path: 'verify-otp',
        loadComponent: () =>
          import('./features/auth/pages/verify-otp/verify-otp.component').then(
            (m) => m.VerifyOtpComponent,
          ),
      },
      {
        path: 'forgot-password',
        loadComponent: () =>
          import('./features/auth/pages/forgot-password/forgot-password.component').then(
            (m) => m.ForgotPasswordComponent,
          ),
      },
    ],
  },

  // ==================== DASHBOARD LAYOUT WITH ROLE-BASED ACCESS ====================
  {
    path: 'dashboard',
    children: [
      // ============ ADMIN DASHBOARD ============
      {
        path: 'admin',
        loadChildren: () =>
          import('../app/dashboards/admin/Admin-dashboard/admin.routes').then((m) => m.adminRoutes),
      },

      // ============ LANDLORD DASHBOARD ============
      {
        path: 'landlord',
        canActivate: [roleGuard],
        data: { expectedRole: 'owner' },
        loadComponent: () =>
          import('./dashboards/landlord/landlord-shell.component').then(
            (m) => m.LandlordShellComponent,
          ),
        children: [
          { path: '', redirectTo: 'overview', pathMatch: 'full' },
          {
            path: 'overview',
            loadComponent: () =>
              import('./dashboards/landlord/pages/overview/overview.component').then(
                (m) => m.LandlordOverviewComponent,
              ),
          },
          {
            path: 'properties',
            loadComponent: () =>
              import('./dashboards/landlord/pages/properties/properties.component').then(
                (m) => m.LandlordPropertiesComponent,
              ),
          },
          {
            path: 'analytics',
            loadComponent: () =>
              import('./dashboards/landlord/pages/analytics/analytics.component').then(
                (m) => m.LandlordAnalyticsComponent,
              ),
          },
          {
            path: 'subscription',
            loadComponent: () =>
              import('./dashboards/landlord/pages/subscription/subscription.component').then(
                (m) => m.LandlordSubscriptionComponent,
              ),
          },
          {
            path: 'profile',
            loadComponent: () =>
              import('./dashboards/landlord/pages/profile/profile.component').then(
                (m) => m.LandlordProfileComponent,
              ),
          },
        ],
      },

      // ============ COMPANY DASHBOARD ============
      {
        path: 'company',
        canActivate: [roleGuard],
        data: { expectedRole: 'company' },
        loadComponent: () =>
          import('./dashboards/company/company-dashboard.component').then(
            (m) => m.CompanyDashboardComponent,
          ),
        children: [
          { path: '', pathMatch: 'full', redirectTo: 'overview' },
          {
            path: 'overview',
            loadComponent: () =>
              import('./dashboards/company/pages/overview/overview.component').then(
                (m) => m.OverviewComponent,
              ),
          },
          {
            path: 'properties',
            loadComponent: () =>
              import('./dashboards/company/pages/properties/properties.component').then(
                (m) => m.PropertiesComponent,
              ),
          },
          {
            path: 'brokers',
            loadComponent: () =>
              import('./dashboards/company/pages/brokers/brokers.component').then(
                (m) => m.BrokersComponent,
              ),
          },
          {
            path: 'performance',
            loadComponent: () =>
              import('./dashboards/company/pages/performance/performance.component').then(
                (m) => m.PerformanceComponent,
              ),
          },
          {
            path: 'inquiries',
            loadComponent: () =>
              import('./dashboards/company/pages/inquiries/inquiries.component').then(
                (m) => m.InquiriesComponent,
              ),
          },
          {
            path: 'ai',
            loadComponent: () =>
              import('./dashboards/company/pages/ai/ai.component').then((m) => m.AiComponent),
          },
          {
            path: 'leases',
            loadComponent: () =>
              import('./dashboards/company/pages/leases/leases.component').then(
                (m) => m.LeasesComponent,
              ),
          },
          {
            path: 'subscriptions',
            loadComponent: () =>
              import('./dashboards/company/pages/subscriptions/subscriptions.component').then(
                (m) => m.SubscriptionsComponent,
              ),
          },
          {
            path: 'settings',
            loadComponent: () =>
              import('./dashboards/company/pages/settings/settings.component').then(
                (m) => m.SettingsComponent,
              ),
          },
        ],
      },

      // ============ BROKER DASHBOARD ============
      {
        path: 'broker',
        canActivate: [roleGuard],
        data: { expectedRole: 'broker' },
        loadComponent: () =>
          import('./dashboards/broker/broker-dashboard.component').then(
            (m) => m.BrokerDashboardComponent,
          ),
        children: [
          {
            path: 'overview',
            loadComponent: () =>
              import('./dashboards/broker/pages/overview/overview.component').then(
                (m) => m.OverviewComponent,
              ),
          },
          {
            path: 'listings',
            loadComponent: () =>
              import('./features/properties/pages/my-listings/my-listings.component').then(
                (m) => m.MyListingsComponent,
              ),
          },
          {
            path: 'performance',
            loadComponent: () =>
              import('./dashboards/broker/pages/performance/performance.component').then(
                (m) => m.PerformanceComponent,
              ),
          },
          {
            path: 'commissions',
            loadComponent: () =>
              import('./dashboards/broker/pages/commissions/commissions.component').then(
                (m) => m.CommissionsComponent,
              ),
          },
        ],
      },

      // ============ TENANT DASHBOARD ============
      {
        path: 'tenant',
        canActivate: [roleGuard],
        data: { expectedRole: 'tenant' },
        loadComponent: () =>
          import('./dashboards/tenant/tenant-dashboard.component').then(
            (m) => m.TenantDashboardComponent,
          ),
        children: [
          {
            path: 'overview',
            loadComponent: () =>
              import('./dashboards/tenant/pages/overview/overview.component').then(
                (m) => m.OverviewComponent,
              ),
          },
          {
            path: 'my-property',
            loadComponent: () =>
              import('./dashboards/tenant/pages/my-property/my-property.component').then(
                (m) => m.MyPropertyComponent,
              ),
          },
          {
            path: 'lease',
            loadComponent: () =>
              import('./dashboards/tenant/pages/lease/lease.component').then(
                (m) => m.LeaseComponent,
              ),
          },
          {
            path: 'maintenance-requests',
            loadComponent: () =>
              import('./dashboards/tenant/pages/maintenance-requests/maintenance-requests.component').then(
                (m) => m.MaintenanceRequestsComponent,
              ),
          },
          {
            path: 'payments',
            loadComponent: () =>
              import('./dashboards/tenant/pages/payments/payments.component').then(
                (m) => m.PaymentsComponent,
              ),
          },
        ],
      },

      // ============ MAINTENANCE COMPANY DASHBOARD ============
      {
        path: 'maintenance',
        loadChildren: () =>
          import('./dashboards/maintenance-company/maintenance.routes').then(
            (m) => m.maintenanceRoutes,
          ),
      },

      // ============ TENANT DASHBOARD (catches /dashboard and /dashboard/*) ============
      {
        path: '',
        loadChildren: () =>
          import('./features/dashboards/tenant-dashboard/dashboard.routes').then(
            (m) => m.dashboardRoutes,
          ),
      },
    ],
  },

  // ==================== CATCH-ALL ROUTE ====================
  {
    path: '**',
    redirectTo: '',
  },
];