import { Routes } from '@angular/router';
import { roleGuard } from './core/guards/role.guard';

export const routes: Routes = [
  // ==================== PUBLIC LAYOUT (Navbar + Footer) ====================
  {
    path: '',
    loadComponent: () =>
      import('./layout/main-layout/main-layout.component').then((m) => m.MainLayoutComponent),
    children: [
      {
        path: '',
        loadComponent: () => import('./features/home/home').then((m) => m.HomeComponent),
      },
    ],
  },

  // ==================== AUTH ROUTES ====================
  {
    path: 'auth',
    children: [
      {
        path: 'login',
        loadComponent: () =>
          import('./features/auth/pages/login/login.component').then((m) => m.LoginComponent),
      },
      {
        path: 'register',
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
    canActivate: [roleGuard],
    children: [
      // ============ ADMIN DASHBOARD ============
      {
        path: 'admin',
        canActivate: [roleGuard],
        data: { expectedRole: 'admin' },
        loadComponent: () =>
          import('./dashboards/admin/admin-dashboard.component').then(
            (m) => m.AdminDashboardComponent,
          ),
        children: [
          {
            path: 'overview',
            loadComponent: () =>
              import('./dashboards/admin/pages/overview/overview.component').then(
                (m) => m.OverviewComponent,
              ),
          },
          {
            path: 'users',
            loadComponent: () =>
              import('./dashboards/admin/pages/users-management/users-management.component').then(
                (m) => m.UsersManagementComponent,
              ),
          },
          {
            path: 'companies',
            loadComponent: () =>
              import('./dashboards/admin/pages/companies-management/companies-management.component').then(
                (m) => m.CompaniesManagementComponent,
              ),
          },
          {
            path: 'moderation',
            loadComponent: () =>
              import('./dashboards/admin/pages/moderation/moderation.component').then(
                (m) => m.ModerationComponent,
              ),
          },
          {
            path: 'reports',
            loadComponent: () =>
              import('./dashboards/admin/pages/reports/reports.component').then(
                (m) => m.ReportsComponent,
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
              import('./dashboards/broker/pages/listings/listings.component').then(
                (m) => m.ListingsComponent,
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
        path: 'maintenance-company',
        canActivate: [roleGuard],
        data: { expectedRole: 'maintenance-company' },
        loadComponent: () =>
          import('./dashboards/maintenance-company/maintenance-company-dashboard.component').then(
            (m) => m.MaintenanceCompanyDashboardComponent,
          ),
        children: [
          {
            path: 'overview',
            loadComponent: () =>
              import('./dashboards/maintenance-company/pages/overview/overview.component').then(
                (m) => m.OverviewComponent,
              ),
          },
          {
            path: 'tickets',
            loadComponent: () =>
              import('./dashboards/maintenance-company/pages/tickets/tickets.component').then(
                (m) => m.TicketsComponent,
              ),
          },
          {
            path: 'work-orders',
            loadComponent: () =>
              import('./dashboards/maintenance-company/pages/work-orders/work-orders.component').then(
                (m) => m.WorkOrdersComponent,
              ),
          },
          {
            path: 'reports',
            loadComponent: () =>
              import('./dashboards/maintenance-company/pages/reports/reports.component').then(
                (m) => m.ReportsComponent,
              ),
          },
        ],
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
