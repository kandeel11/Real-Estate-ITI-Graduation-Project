import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, catchError } from 'rxjs';
import { environment } from '../../../../../environments/environment';

export interface ActiveLeaseInfo {
  id: string;
  status: 'Active' | 'Inactive';
  propertyAddress: string;
  monthlyRent: number;
  startDate: string;
  endDate: string;
  daysUntilExpiry: number;
}

export interface TicketStats {
  open: number;
  inProgress: number;
  completed: number;
  waitingForDecision: number;
  total: number;
}

export interface DashboardSummary {
  tenantName: string;
  lease: ActiveLeaseInfo | null;
  ticketStats: TicketStats;
  notifications: { unreadCount: number };
}

export interface ActivityItem {
  id: string;
  type: string;
  title: string;
  body: string;
  createdAt: string;
  isRead: boolean;
}

const MOCK_SUMMARY: DashboardSummary = {
  tenantName: 'Sarah Johnson',
  lease: {
    id: 'lease-001',
    status: 'Active',
    propertyAddress: '42 Oak Street, Suite 3B, New York, NY 10001',
    monthlyRent: 2800,
    startDate: '2024-03-01',
    endDate: '2025-09-30',
    daysUntilExpiry: 45,
  },
  ticketStats: { open: 2, inProgress: 1, completed: 5, waitingForDecision: 1, total: 9 },
  notifications: { unreadCount: 3 },
};

const MOCK_ACTIVITY: ActivityItem[] = [
  {
    id: '1', type: 'maintenance', title: 'Maintenance ticket updated',
    body: 'Your HVAC ticket has been assigned to a technician.',
    createdAt: new Date(Date.now() - 7200000).toISOString(), isRead: false,
  },
  {
    id: '2', type: 'lease', title: 'Lease renewal reminder',
    body: 'Your lease expires in 45 days. Contact your landlord to discuss renewal.',
    createdAt: new Date(Date.now() - 86400000).toISOString(), isRead: false,
  },
  {
    id: '3', type: 'maintenance', title: 'Ticket resolved',
    body: 'Your ticket #TCK-0004 (Broken blinds) has been marked as completed.',
    createdAt: new Date(Date.now() - 259200000).toISOString(), isRead: true,
  },
  {
    id: '4', type: 'notification', title: 'Message from landlord',
    body: 'Your landlord sent a message regarding the upcoming property inspection.',
    createdAt: new Date(Date.now() - 432000000).toISOString(), isRead: true,
  },
  {
    id: '5', type: 'payment', title: 'Rent payment confirmed',
    body: 'Your rent payment of $2,800 for this month has been confirmed.',
    createdAt: new Date(Date.now() - 604800000).toISOString(), isRead: true,
  },
];

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  getDashboardSummary(): Observable<DashboardSummary> {
    // TODO: replace with real API call
    return of(MOCK_SUMMARY);
    // return this.http.get<DashboardSummary>(`${this.base}/tenant/dashboard/summary`)
    //   .pipe(catchError(() => of(MOCK_SUMMARY)));
  }

  getActivityFeed(pageSize = 5): Observable<ActivityItem[]> {
    // TODO: replace with real API call
    return of(MOCK_ACTIVITY.slice(0, pageSize));
    // return this.http
    //   .get<ActivityItem[]>(`${this.base}/tenant/dashboard/activity`, { params: { pageSize } })
    //   .pipe(catchError(() => of([])));
  }
}
