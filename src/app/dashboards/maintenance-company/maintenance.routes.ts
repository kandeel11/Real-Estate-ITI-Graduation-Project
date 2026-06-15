import { Routes } from '@angular/router';

export const maintenanceRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./layout/maintenance-layout.component').then((m) => m.MaintenanceLayoutComponent),
    children: [
      { path: '', redirectTo: 'overview', pathMatch: 'full' },
      {
        path: 'overview',
        loadComponent: () => import('./pages/overview.component').then((m) => m.OverviewComponent),
      },
      {
        path: 'tickets',
        loadComponent: () =>
          import('./pages/tickets/tickets.component').then((m) => m.MaintenanceTicketsComponent),
      },
      // مكان للصفحات الجاية: schedule, customers, categories, earnings...
    ],
  },
];
