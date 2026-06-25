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
        loadComponent: () =>
          import('./pages/overview.component').then((m) => m.OverviewComponent),
      },
      {
        path: 'tickets',
        loadComponent: () =>
          import('./pages/tickets/tickets.component').then((m) => m.MaintenanceTicketsComponent),
      },
      {
        path: 'tickets/:id',
        loadComponent: () =>
          import('./pages/tickets/ticket-detail/ticket-detail.component').then(
            (m) => m.TicketDetailComponent,
          ),
      },
      {
        path: 'profile',
        loadComponent: () =>
          import('./pages/profile/profile.component').then((m) => m.MaintenanceProfileComponent),
      },
    ],
  },
];
