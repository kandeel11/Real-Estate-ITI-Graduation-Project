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

      // Placeholder redirects until pages are built
      { path: 'analytics', redirectTo: 'overview', pathMatch: 'full' },
      { path: 'users', redirectTo: 'overview', pathMatch: 'full' },
      { path: 'companies', redirectTo: 'overview', pathMatch: 'full' },
      { path: 'approvals', redirectTo: 'overview', pathMatch: 'full' },
      { path: 'reports', redirectTo: 'overview', pathMatch: 'full' },
      { path: 'settings', redirectTo: 'overview', pathMatch: 'full' },
    ],
  },
];
