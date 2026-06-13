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

      // مكان للصفحات الجاية (users, companies, approvals, etc.)
    ],
  },
];
