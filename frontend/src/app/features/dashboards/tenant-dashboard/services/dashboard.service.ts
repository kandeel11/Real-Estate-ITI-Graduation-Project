import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { environment } from '../../../../../environments/environment';

// ── API types ────────────────────────────────────────────────────────────────

export interface ApiActiveLease {
  leaseId: string;
  propertyId: string;
  monthlyRent: number;
  startDate: string;
  endDate: string;
  daysUntilExpiry: number;
  isExpiringSoon: boolean;
}

export interface ApiTicketStats {
  total: number;
  draft: number;
  submitted: number;
  underAiReview: number;
  waitingForDecision: number;
  assignedToVendor: number;
  inProgress: number;
  awaitingPayment: number;
  completed: number;
  cancelled: number;
  rejected: number;
}

export interface ApiDashboardSummary {
  tenant: { id: string; email: string; role: string };
  activeLease: ApiActiveLease | null;
  ticketStats: ApiTicketStats;
  notifications: { unreadCount: number };
  inquiries: { pending: number; total: number };
}

// ── Kept for activity feed (still mock) ─────────────────────────────────────

export interface ActivityItem {
  id: string;
  type: string;
  title: string;
  body: string;
  createdAt: string;
  isRead: boolean;
}

const MOCK_ACTIVITY: ActivityItem[] = [
  {
    id: '1', type: 'maintenance', title: 'Maintenance ticket updated',
    body: 'Your ticket has been assigned to a technician.',
    createdAt: new Date(Date.now() - 7200000).toISOString(), isRead: false,
  },
  {
    id: '2', type: 'lease', title: 'Lease renewal reminder',
    body: 'Your lease expires soon. Contact your landlord to discuss renewal.',
    createdAt: new Date(Date.now() - 86400000).toISOString(), isRead: false,
  },
  {
    id: '3', type: 'maintenance', title: 'Ticket resolved',
    body: 'Your maintenance ticket has been marked as completed.',
    createdAt: new Date(Date.now() - 259200000).toISOString(), isRead: true,
  },
];

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  getDashboardSummary(): Observable<ApiDashboardSummary> {
    return this.http.get<ApiDashboardSummary>(`${this.base}/tenant/dashboard/summary`);
  }

  getActivityFeed(pageSize = 5): Observable<ActivityItem[]> {
    // TODO: replace with real API call
    return of(MOCK_ACTIVITY.slice(0, pageSize));
  }
}
