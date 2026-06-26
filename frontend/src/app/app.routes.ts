import { Routes } from '@angular/router';
import {
  roleGuard,
  multiRoleGuard,
  dashboardRedirectGuard,
  ownerLockGuard,
  guestGuard,
} from './core/guards/role.guard';

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
        canActivate: [multiRoleGuard, ownerLockGuard],
        data: { expectedRoles: ['owner', 'broker'] },
        loadComponent: () =>
          import('./features/properties/pages/my-listings/my-listings.component').then(
            (m) => m.MyListingsComponent,
          ),
      },
      {
        path: 'properties',
        canActivate: [ownerLockGuard],
        loadComponent: () =>
          import('./features/properties/pages/browse/browse.component').then(
            (m) => m.BrowseComponent,
          ),
      },
      {
        path: 'properties/:id',
        canActivate: [ownerLockGuard],
        loadComponent: () =>
          import('./features/properties/pages/detail/detail.component').then(
            (m) => m.DetailComponent,
          ),
      },
      {
        path: 'services',
        canActivate: [ownerLockGuard],
        loadComponent: () =>
          import('./features/services/services.component').then((m) => m.ServicesComponent),
      },
      {
        path: 'contact',
        canActivate: [ownerLockGuard],
        loadComponent: () =>
          import('./features/contact/contact.component').then((m) => m.ContactComponent),
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
        canActivate: [roleGuard],
        data: { expectedRole: 'admin' },
        loadChildren: () =>
          import('../app/features/admin/Admin-dashboard/admin.routes').then((m) => m.adminRoutes),
      },

      // ============ LANDLORD DASHBOARD ============
      {
        path: 'landlord',
        canActivate: [roleGuard],
        data: { expectedRole: 'owner' },
        loadComponent: () =>
          import('./features/dashboards/landlord/landlord-shell.component').then(
            (m) => m.LandlordShellComponent,
          ),
        children: [
          { path: '', redirectTo: 'overview', pathMatch: 'full' },
          {
            path: 'overview',
            loadComponent: () =>
              import('./features/dashboards/landlord/pages/overview/overview.component').then(
                (m) => m.LandlordOverviewComponent,
              ),
          },
          {
            path: 'properties',
            loadComponent: () =>
              import('./features/dashboards/landlord/pages/properties/properties.component').then(
                (m) => m.LandlordPropertiesComponent,
              ),
          },
          {
            path: 'analytics',
            loadComponent: () =>
              import('./features/dashboards/landlord/pages/analytics/analytics.component').then(
                (m) => m.LandlordAnalyticsComponent,
              ),
          },
          {
            path: 'subscription',
            loadComponent: () =>
              import('./features/dashboards/landlord/pages/subscription/subscription.component').then(
                (m) => m.LandlordSubscriptionComponent,
              ),
          },
          {
            path: 'profile',
            loadComponent: () =>
              import('./features/dashboards/landlord/pages/profile/profile.component').then(
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
          import('./features/dashboards/company/company-dashboard.component').then(
            (m) => m.CompanyDashboardComponent,
          ),
        children: [
          { path: '', pathMatch: 'full', redirectTo: 'overview' },
          {
            path: 'overview',
            loadComponent: () =>
              import('./features/dashboards/company/pages/overview/overview.component').then(
                (m) => m.OverviewComponent,
              ),
          },
          {
            path: 'properties',
            loadComponent: () =>
              import('./features/dashboards/company/pages/properties/properties.component').then(
                (m) => m.PropertiesComponent,
              ),
          },
          {
            path: 'brokers',
            loadComponent: () =>
              import('./features/dashboards/company/pages/brokers/brokers.component').then(
                (m) => m.BrokersComponent,
              ),
          },
          {
            path: 'performance',
            loadComponent: () =>
              import('./features/dashboards/company/pages/performance/performance.component').then(
                (m) => m.PerformanceComponent,
              ),
          },
          {
            path: 'inquiries',
            loadComponent: () =>
              import('./features/dashboards/company/pages/inquiries/inquiries.component').then(
                (m) => m.InquiriesComponent,
              ),
          },
          {
            path: 'ai',
            loadComponent: () =>
              import('./features/dashboards/company/pages/ai/ai.component').then(
                (m) => m.AiComponent,
              ),
          },
          {
            path: 'leases',
            loadComponent: () =>
              import('./features/dashboards/company/pages/leases/leases.component').then(
                (m) => m.LeasesComponent,
              ),
          },
          {
            path: 'tickets',
            loadComponent: () =>
              import('./features/dashboards/company/pages/tickets/tickets.component').then(
                (m) => m.CompanyTicketsComponent,
              ),
          },
          {
            path: 'payments',
            loadComponent: () =>
              import('./features/dashboards/company/pages/payments/payments.component').then(
                (m) => m.CompanyPaymentsComponent,
              ),
          },
          {
            path: 'subscriptions',
            loadComponent: () =>
              import('./features/dashboards/company/pages/subscriptions/subscriptions.component').then(
                (m) => m.SubscriptionsComponent,
              ),
          },
          {
            path: 'settings',
            loadComponent: () =>
              import('./features/dashboards/company/pages/settings/settings.component').then(
                (m) => m.SettingsComponent,
              ),
          },
          {
            path: 'pipeline',
            loadComponent: () =>
              import('./features/dashboards/company/pages/pipeline/pipeline.component').then(
                (m) => m.PipelineComponent,
              ),
          },
          {
            path: 'wallet',
            loadComponent: () =>
              import('./features/dashboards/company/pages/wallet/wallet.component').then(
                (m) => m.CompanyWalletComponent,
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
          import('./features/dashboards/broker/broker-dashboard.component').then(
            (m) => m.BrokerDashboardComponent,
          ),
        children: [
          { path: '', redirectTo: 'overview', pathMatch: 'full' },
          {
            path: 'overview',
            loadComponent: () =>
              import('./features/dashboards/broker/pages/overview/overview.component').then(
                (m) => m.OverviewComponent,
              ),
          },
          {
            path: 'properties',
            loadComponent: () =>
              import('./features/dashboards/broker/pages/properties/properties.component').then(
                (m) => m.PropertiesComponent,
              ),
          },
          {
            path: 'leads',
            loadComponent: () =>
              import('./features/dashboards/broker/pages/leads/leads.component').then(
                (m) => m.LeadsComponent,
              ),
          },
          {
            path: 'inquiries',
            loadComponent: () =>
              import('./features/dashboards/broker/pages/inquiries/inquiries.component').then(
                (m) => m.InquiriesComponent,
              ),
          },
          {
            path: 'messages',
            loadComponent: () =>
              import('./features/dashboards/broker/pages/messages/messages.component').then(
                (m) => m.MessagesComponent,
              ),
          },
          {
            path: 'performance',
            loadComponent: () =>
              import('./features/dashboards/broker/pages/performance/performance.component').then(
                (m) => m.PerformanceComponent,
              ),
          },
          {
            path: 'leases',
            loadComponent: () =>
              import('./features/dashboards/broker/pages/leases/leases.component').then(
                (m) => m.LeasesComponent,
              ),
          },
          {
            path: 'notifications',
            loadComponent: () =>
              import('./features/dashboards/broker/pages/notifications/notifications.component').then(
                (m) => m.BrokerNotificationsComponent,
              ),
          },
          {
            path: 'subscription',
            loadComponent: () =>
              import('./features/dashboards/broker/pages/subscription/subscription.component').then(
                (m) => m.BrokerSubscriptionComponent,
              ),
          },
          {
            path: 'profile',
            loadComponent: () =>
              import('./features/dashboards/broker/pages/profile/profile.component').then(
                (m) => m.ProfileComponent,
              ),
          },
          {
            path: 'payments',
            loadComponent: () =>
              import('./features/dashboards/broker/pages/payments/payments.component').then(
                (m) => m.BrokerPaymentsComponent,
              ),
          },
        ],
      },

      // ============ OWNER DASHBOARD ============
      {
        path: 'owner',
        canActivate: [roleGuard],
        data: { expectedRole: 'owner' },
        loadComponent: () =>
          import('./features/dashboards/owner-dashboard/owner-dashboard.component').then(
            (m) => m.OwnerDashboardComponent,
          ),
        children: [
          { path: '', redirectTo: 'overview', pathMatch: 'full' },
          {
            path: 'overview',
            loadComponent: () =>
              import('./features/dashboards/owner-dashboard/pages/overview/overview.component').then(
                (m) => m.OverviewComponent,
              ),
          },
          {
            path: 'properties',
            loadComponent: () =>
              import('./features/dashboards/owner-dashboard/pages/properties/properties.component').then(
                (m) => m.PropertiesComponent,
              ),
          },
          {
            path: 'leads',
            loadComponent: () =>
              import('./features/dashboards/owner-dashboard/pages/leads/leads.component').then(
                (m) => m.LeadsComponent,
              ),
          },
          {
            path: 'inquiries',
            loadComponent: () =>
              import('./features/dashboards/owner-dashboard/pages/inquiries/inquiries.component').then(
                (m) => m.InquiriesComponent,
              ),
          },
          {
            path: 'messages',
            loadComponent: () =>
              import('./features/dashboards/owner-dashboard/pages/messages/messages.component').then(
                (m) => m.MessagesComponent,
              ),
          },
          {
            path: 'performance',
            loadComponent: () =>
              import('./features/dashboards/owner-dashboard/pages/performance/performance.component').then(
                (m) => m.PerformanceComponent,
              ),
          },
          {
            path: 'leases',
            loadComponent: () =>
              import('./features/dashboards/owner-dashboard/pages/leases/leases.component').then(
                (m) => m.LeasesComponent,
              ),
          },
          {
            path: 'maintenance',
            loadComponent: () =>
              import('./features/dashboards/owner-dashboard/pages/maintenance/maintenance.component').then(
                (m) => m.MaintenanceComponent,
              ),
          },
          {
            path: 'maintenance/:id',
            loadComponent: () =>
              import('./features/dashboards/owner-dashboard/pages/maintenance/ticket-detail/ticket-detail.component').then(
                (m) => m.OwnerTicketDetailComponent,
              ),
          },
          {
            path: 'notifications',
            loadComponent: () =>
              import('./features/dashboards/owner-dashboard/pages/notifications/notifications.component').then(
                (m) => m.OwnerNotificationsComponent,
              ),
          },
          {
            path: 'subscription',
            loadComponent: () =>
              import('./features/dashboards/owner-dashboard/pages/subscription/subscription.component').then(
                (m) => m.OwnerSubscriptionComponent,
              ),
          },
          {
            path: 'profile',
            loadComponent: () =>
              import('./features/dashboards/owner-dashboard/pages/profile/profile.component').then(
                (m) => m.ProfileComponent,
              ),
          },
          {
            path: 'wallet',
            loadComponent: () =>
              import('./features/dashboards/owner-dashboard/pages/wallet/wallet.component').then(
                (m) => m.OwnerWalletComponent,
              ),
          },
        ],
      },

      // ============ TENANT DASHBOARD ============
      {
        path: 'tenant',
        canActivate: [roleGuard],
        data: { expectedRole: 'tenant' },
        loadChildren: () =>
          import('./features/dashboards/tenant-dashboard/dashboard.routes').then(
            (m) => m.dashboardRoutes,
          ),
      },

      // ============ MAINTENANCE COMPANY DASHBOARD ============
      {
        path: 'maintenance',
        canActivate: [roleGuard],
        data: { expectedRole: 'maintenance-company' },
        loadChildren: () =>
          import('./features/dashboards/vendor-dashboard/maintenance.routes').then(
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
