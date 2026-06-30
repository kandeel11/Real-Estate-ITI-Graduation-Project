import { Injectable, inject } from '@angular/core';
import { Observable, of, map } from 'rxjs';
import {
  AdminUser,
  Company,
  DashboardStats,
  MonthlyRevenuePoint,
  RentPayment,
} from '../Models';
import { PaymentService, PlatformDashboard } from '../../../../core/services/payment.service';
import { RentPayment as CoreRentPayment } from '../../../../core/models/payment.model';

const MONTH_LABELS = ['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec'];

@Injectable({ providedIn: 'root' })
export class AdminDashboardService {
  private paymentService = inject(PaymentService);

  // ── Real financial stats from platform-dashboard ──────────────────────────
  getStats(): Observable<DashboardStats> {
    return this.paymentService.getPlatformDashboard().pipe(
      map((p: PlatformDashboard) => {
        const revenue = p.rent.totalCollected;
        const commission = p.platform.totalCommissionEarned;
        return {
          totalUsers: 0,
          usersGrowth: 0,
          totalCompanies: 0,
          companiesGrowth: 0,
          pendingProperties: 0,
          monthlyRevenue: revenue,
          revenueGrowth: 0,
          totalWithdrawn: p.withdrawals.totalWithdrawn,
          platformCommission: commission,
          netYield: revenue > 0
            ? Math.round((commission / revenue) * 100 * 10) / 10
            : 0,
        };
      })
    );
  }

  // ── Monthly revenue from real payments grouped by paidAt month ────────────
  getMonthlyRevenue(): Observable<MonthlyRevenuePoint[]> {
    return this.paymentService.getAllPayments().pipe(
      map((payments: CoreRentPayment[]) => {
        const paidPayments = payments.filter(
          (p: CoreRentPayment) => (p.status === 'Paid' || p.status === 'PaidExternally') && p.paidAt
        );

        const now = new Date();
        const result: MonthlyRevenuePoint[] = [];
        for (let i = 11; i >= 0; i--) {
          const d = new Date(now.getFullYear(), now.getMonth() - i, 1);
          const y = d.getFullYear();
          const m = d.getMonth();
          const total = paidPayments
            .filter((p: CoreRentPayment) => {
              const pd = new Date(p.paidAt!);
              return pd.getFullYear() === y && pd.getMonth() === m;
            })
            .reduce((sum: number, p: CoreRentPayment) => sum + p.amount, 0);
          result.push({ month: MONTH_LABELS[m], amount: total });
        }
        return result;
      })
    );
  }

  // ── Recent payments from real API ─────────────────────────────────────────
  getRecentPayments(): Observable<RentPayment[]> {
    return this.paymentService.getAllPayments().pipe(
      map((payments: CoreRentPayment[]) =>
        payments
          .sort((a: CoreRentPayment, b: CoreRentPayment) => {
            const da = a.paidAt ? new Date(a.paidAt).getTime() : new Date(a.dueDate).getTime();
            const db = b.paidAt ? new Date(b.paidAt).getTime() : new Date(b.dueDate).getTime();
            return db - da;
          })
          .slice(0, 5)
          .map((p: CoreRentPayment): RentPayment => ({
            id: p.id,
            propertyId: p.propertyId ?? '',
            propertyTitle: p.propertyTitle ?? 'Unknown Property',
            tenantName: p.tenantName ?? 'Unknown Tenant',
            amount: p.amount,
            dueDate: p.dueDate,
            paidDate: p.paidAt ?? undefined,
            status: this.mapStatus(p.status),
          }))
      )
    );
  }

  private mapStatus(status: string): 'paid' | 'pending' | 'overdue' {
    if (status === 'Paid' || status === 'PaidExternally') return 'paid';
    if (status === 'Overdue' || status === 'Late') return 'overdue';
    return 'pending';
  }

  // ── Property approval / rejection ─────────────────────────────────────────
  approveProperty(id: string): Observable<void> {
    console.log('[AdminDashboard] approveProperty', id);
    return of(undefined);
  }

  rejectProperty(id: string, reason?: string): Observable<void> {
    console.log('[AdminDashboard] rejectProperty', id, reason);
    return of(undefined);
  }

  // ── Not used by dashboard — kept for other consumers ─────────────────────
  getLatestUsers(): Observable<AdminUser[]> {
    return of([]);
  }

  getTopCompanies(): Observable<Company[]> {
    return of([]);
  }
}
