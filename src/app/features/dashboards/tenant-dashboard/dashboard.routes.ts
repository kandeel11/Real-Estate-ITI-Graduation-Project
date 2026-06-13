import { Routes } from '@angular/router';
import { TDashboardComponent } from './t-dashboard/t-dashboard.component';
import { roleGuard } from '../../../core/guards/role.guard';

export const dashboardRoutes: Routes = [
  {
    path: '',
    component: TDashboardComponent,
    // Tenant-only: other roles have their own dashboards and get bounced
    canActivate: [roleGuard],
    data: { expectedRole: 'tenant' },
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./pages/overview/overview.component').then(m => m.OverviewComponent),
      },
      {
        path: 'property',
        loadComponent: () =>
          import('./pages/my-property/my-property.component').then(m => m.MyPropertyComponent),
      },
      {
        path: 'lease',
        loadComponent: () =>
          import('./pages/my-lease/my-lease.component').then(m => m.MyLeaseComponent),
      },
      {
        path: 'maintenance',
        loadComponent: () =>
          import('./pages/maintenance/maintenance.component').then(m => m.MaintenanceComponent),
      },
      {
        path: 'maintenance/create',
        loadComponent: () =>
          import('./pages/maintenance/create-ticket/create-ticket.component').then(m => m.CreateTicketComponent),
      },
      {
        path: 'maintenance/:id',
        loadComponent: () =>
          import('./pages/maintenance/ticket-detail/ticket-detail.component').then(m => m.TicketDetailComponent),
      },
      {
        path: 'notifications',
        loadComponent: () =>
          import('./pages/notifications/notifications.component').then(m => m.NotificationsComponent),
      },
      {
        path: 'ratings',
        loadComponent: () =>
          import('./pages/ratings/ratings.component').then(m => m.RatingsComponent),
      },
      {
        path: 'profile',
        loadComponent: () =>
          import('./pages/profile/profile.component').then(m => m.ProfileComponent),
      },
      {
        path: 'ai',
        loadComponent: () =>
          import('./pages/ai/ai.component').then(m => m.AiComponent),
      },
      {
        path: 'subscription',
        loadComponent: () =>
          import('../../subscription/pages/subscription-plans/subscription-plans.component').then(
            m => m.SubscriptionPlansComponent
          ),
      },
    ],
  },
];
