import { Routes } from '@angular/router';

export const adminRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./layout/admin-layout').then((m) => m.AdminLayoutComponent),
    children: [
      { path: '', redirectTo: 'overview', pathMatch: 'full' },
      {
        path: 'overview',
        loadComponent: () =>
          import('./pages/dashboard/dashboard').then((m) => m.DashboardComponent),
      },
      {
        path: 'maintenance-companies',
        loadComponent: () =>
          import('./pages/maintenance-companies/maintenance-companies.component').then(
            (m) => m.MaintenanceCompaniesComponent,
          ),
      },
      {
        path: 'portfolio',
        loadComponent: () =>
          import('./pages/portfolio/portfolio.component').then((m) => m.PortfolioComponent),
      },
      {
        path: 'users',
        loadComponent: () => import('./pages/users/users.component').then((m) => m.UsersComponent),
      },
      {
        path: 'market-trends',
        loadComponent: () =>
          import('./pages/market-trends/market-trends.component').then(
            (m) => m.MarketTrendsComponent,
          ),
      },
      {
        path: 'settings',
        loadComponent: () =>
          import('./pages/settings/settings.component').then((m) => m.SettingsComponent),
      },
      {
        path: 'ai',
        loadComponent: () =>
          import('./pages/ai-chat/ai-chat.component').then((m) => m.AiChatComponent),
      },

      {
        path: 'companies',
        loadComponent: () =>
          import('./pages/companies/companies.component').then((m) => m.CompaniesComponent),
      },
      {
        path: 'tickets',
        loadComponent: () =>
          import('./pages/tickets/tickets.component').then((m) => m.AdminTicketsComponent),
      },
      {
        path: 'payments',
        loadComponent: () =>
          import('./pages/payments/payments.component').then((m) => m.AdminPaymentsComponent),
      },
      {
        path: 'withdrawals',
        loadComponent: () =>
          import('./pages/withdrawals/withdrawals.component').then(
            (m) => m.AdminWithdrawalsComponent,
          ),
      },
      {
        path: 'platform',
        loadComponent: () =>
          import('./pages/platform-dashboard/platform-dashboard.component').then(
            (m) => m.AdminPlatformDashboardComponent,
          ),
      },
      {
        path: 'disputes',
        loadComponent: () =>
          import('./pages/disputes/disputes.component').then((m) => m.AdminDisputesComponent),
      },
      {
        path: 'disputes/:id',
        loadComponent: () =>
          import('./pages/disputes/dispute-detail/dispute-detail.component').then(
            (m) => m.DisputeDetailComponent,
          ),
      },
      {
        path: 'ownership',
        loadComponent: () =>
          import('./pages/ownership/ownership.component').then((m) => m.OwnershipComponent),
      },
      // Placeholder redirects until pages are built
      { path: 'approvals', redirectTo: 'overview', pathMatch: 'full' },
      { path: 'reports', redirectTo: 'overview', pathMatch: 'full' },
    ],
  },
];
