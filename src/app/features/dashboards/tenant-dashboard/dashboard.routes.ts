import { Routes } from '@angular/router';
// import { TenantDashboardLayoutComponent } from '../../dashboard/tenant-dashboard.component';
import { TDashboardComponent } from './t-dashboard/t-dashboard.component';
import { authGuard } from '../../../core/guards/role.guard';

export const dashboardRoutes: Routes = [
  {
    path: '',
    component: TDashboardComponent,
    canActivate: [authGuard],
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
    ],
  },
];
