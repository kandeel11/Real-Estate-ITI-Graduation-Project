import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, switchMap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { API_ENDPOINTS } from '../constants/api-endpoints';
import {
  RentPayment,
  ProcessPaymentRequest,
  ProcessPaymentResponse,
  GenerateMonthlyResponse,
  PaymentSchedule,
  BrokerLeaseSchedule,
  SubscriptionCheckoutRequest,
  SubscriptionCheckoutResponse,
  SelectPlanRequest,
  SelectPlanResponse,
  StartCheckoutOptions,
} from '../models/payment.model';

// ── Admin platform dashboard ──────────────────────────────────────────────────
export interface PaymentSummaryDto {
  totalCollected: number;
  totalPending: number;
  totalLateFees: number;
  totalOverduePayments: number;
  totalRentCommission: number;
}

export interface PlatformDashboard {
  rent: {
    totalCollected: number;
    totalCommission: number;
    commissionRate: string;
    totalPending: number;
    overdueCount: number;
    totalLateFees: number;
  };
  maintenance: {
    totalCollected: number;
    totalCommission: number;
    commissionRate: string;
    totalNetToVendors: number;
    paidPaymentsCount: number;
  };
  withdrawals: {
    totalWithdrawn: number;
    totalCommission: number;
    commissionRate: string;
    totalNetPaidOut: number;
    pendingCount: number;
    pendingAmount: number;
    completedCount: number;
  };
  platform: {
    totalCommissionEarned: number;
    breakdown: {
      fromRent: number;
      fromMaintenance: number;
      fromWithdrawals: number;
    };
  };
}

@Injectable({ providedIn: 'root' })
export class PaymentService {
  private http = inject(HttpClient);
  private base = environment.apiUrl;

  generateMonthly(targetMonth?: string): Observable<GenerateMonthlyResponse> {
    let params = new HttpParams();
    if (targetMonth) params = params.set('targetMonth', targetMonth);
    return this.http.post<GenerateMonthlyResponse>(
      `${this.base}${API_ENDPOINTS.PAYMENTS.GENERATE_MONTHLY}`,
      {},
      { params },
    );
  }

  getAllPayments(leaseId?: string): Observable<RentPayment[]> {
    let params = new HttpParams();
    if (leaseId) params = params.set('leaseId', leaseId);
    return this.http.get<RentPayment[]>(`${this.base}${API_ENDPOINTS.PAYMENTS.GET_ALL}`, {
      params,
    });
  }

  getPaymentById(id: string): Observable<RentPayment> {
    return this.http.get<RentPayment>(`${this.base}${API_ENDPOINTS.PAYMENTS.GET_BY_ID(id)}`);
  }

  processPayment(id: string, body: ProcessPaymentRequest): Observable<ProcessPaymentResponse> {
    return this.http.post<ProcessPaymentResponse>(
      `${this.base}${API_ENDPOINTS.PAYMENTS.PROCESS(id)}`,
      body,
    );
  }

  getOverduePayments(): Observable<RentPayment[]> {
    return this.http.get<RentPayment[]>(`${this.base}${API_ENDPOINTS.PAYMENTS.GET_OVERDUE}`);
  }

  applyLateFee(id: string): Observable<RentPayment> {
    return this.http.post<RentPayment>(
      `${this.base}${API_ENDPOINTS.PAYMENTS.APPLY_LATE_FEE(id)}`,
      {},
    );
  }

  getReceipt(id: string): Observable<{ receiptUrl: string }> {
    return this.http.get<{ receiptUrl: string }>(
      `${this.base}${API_ENDPOINTS.PAYMENTS.GET_RECEIPT(id)}`,
    );
  }

  getPaymentSchedule(leaseId: string): Observable<PaymentSchedule> {
    return this.http.get<PaymentSchedule>(
      `${this.base}${API_ENDPOINTS.LEASE_SCHEDULE.GET(leaseId)}`,
    );
  }

  /** Tenant's own payments — GET /api/tenant/payments */
  getTenantPayments(): Observable<RentPayment[]> {
    return this.http.get<RentPayment[]>(`${this.base}${API_ENDPOINTS.TENANT_PAYMENTS.GET_ALL}`);
  }

  /**
   * Start a Stripe PaymentIntent for a rent installment.
   * POST /api/payments/{id}/initiate
   * Returns the clientSecret + publishableKey to mount Stripe Elements.
   */
  initiatePayment(id: string): Observable<{ clientSecret: string; paymentIntentId: string; publishableKey: string }> {
    return this.http.post<{ clientSecret: string; paymentIntentId: string; publishableKey: string }>(
      `${this.base}/payments/${id}/initiate`, {}
    );
  }

  /** Tenant's payment schedule — GET /api/tenant/payments/schedule (no leaseId needed) */
  getTenantPaymentSchedule(): Observable<PaymentSchedule> {
    return this.http.get<PaymentSchedule>(`${this.base}${API_ENDPOINTS.TENANT_PAYMENTS.GET_SCHEDULE}`);
  }

  /** Owner's incoming rent payments — GET /api/owners/me/payments */
  getOwnerPayments(): Observable<RentPayment[]> {
    return this.http.get<RentPayment[]>(`${this.base}${API_ENDPOINTS.OWNER_PAYMENTS.GET_ALL}`);
  }

  /** Role-aware shortcut — GET /api/payments/my */
  getMyPayments(): Observable<RentPayment[]> {
    return this.http.get<RentPayment[]>(`${this.base}${API_ENDPOINTS.PAYMENTS.GET_MY}`);
  }

  /** Admin financial summary — GET /api/payments/summary */
  getPaymentSummary(): Observable<PaymentSummaryDto> {
    return this.http.get<PaymentSummaryDto>(`${this.base}${API_ENDPOINTS.PAYMENTS.GET_SUMMARY}`);
  }

  /** Admin platform earnings — GET /api/payments/platform-dashboard */
  getPlatformDashboard(): Observable<PlatformDashboard> {
    return this.http.get<PlatformDashboard>(`${this.base}/payments/platform-dashboard`);
  }

  /**
   * Confirms a rent Checkout Session after Stripe redirects back.
   * POST /api/payments/rent/confirm — marks payment Paid + credits wallet.
   */
  confirmRentPayment(sessionId: string): Observable<{ paymentStatus: string | null }> {
    return this.http.post<{ paymentStatus: string | null }>(
      `${this.base}/payments/rent/confirm`,
      { sessionId },
    );
  }

  /** Owner / company_manager / admin waive late fee — POST /api/payments/{id}/waive-late-fee */
  waiveLateFee(id: string): Observable<void> {
    return this.http.post<void>(
      `${this.base}${API_ENDPOINTS.PAYMENTS.WAIVE_LATE_FEE(id)}`,
      {},
    );
  }

  /** Admin refund — POST /api/payments/{id}/refund */
  refundPayment(id: string, reason: string): Observable<void> {
    return this.http.post<void>(`${this.base}${API_ENDPOINTS.PAYMENTS.REFUND(id)}`, { reason });
  }

  /** Broker payments across all managed leases — GET /api/brokers/me/payments */
  getBrokerPayments(): Observable<RentPayment[]> {
    return this.http.get<RentPayment[]>(`${this.base}${API_ENDPOINTS.BROKER_PAYMENTS.GET_ALL}`);
  }

  /** Broker multi-lease schedule — GET /api/brokers/me/payments/schedule */
  getBrokerPaymentSchedules(): Observable<BrokerLeaseSchedule[]> {
    return this.http.get<BrokerLeaseSchedule[]>(`${this.base}${API_ENDPOINTS.BROKER_PAYMENTS.GET_SCHEDULE}`);
  }

  /** Company payments across all leases — GET /api/companies/mine/payments */
  getCompanyPayments(): Observable<RentPayment[]> {
    return this.http.get<RentPayment[]>(`${this.base}${API_ENDPOINTS.COMPANY_PAYMENTS.GET_ALL}`);
  }

  /** Company multi-lease schedule — GET /api/companies/mine/payments/schedule */
  getCompanyPaymentSchedules(): Observable<BrokerLeaseSchedule[]> {
    return this.http.get<BrokerLeaseSchedule[]>(`${this.base}${API_ENDPOINTS.COMPANY_PAYMENTS.GET_SCHEDULE}`);
  }

  /** Step 1 — create a PendingPayment subscription for the chosen plan. */
  selectPlan(body: SelectPlanRequest): Observable<SelectPlanResponse> {
    return this.http.post<SelectPlanResponse>(
      `${this.base}${API_ENDPOINTS.SUBSCRIPTIONS.SELECT_PLAN}`,
      body,
    );
  }

  /**
   * Confirm a subscription checkout on the success redirect — finalizes the
   * upgrade without depending on the Stripe webhook being forwarded locally.
   * POST /api/payments/subscriptions/confirm
   */
  confirmSubscription(sessionId: string): Observable<{ paymentStatus: string | null }> {
    return this.http.post<{ paymentStatus: string | null }>(
      `${this.base}/payments/subscriptions/confirm`,
      { sessionId },
    );
  }

  /** Step 2 — initiate Stripe checkout — POST /api/payments/subscriptions/checkout */
  checkoutSubscription(body: SubscriptionCheckoutRequest): Observable<SubscriptionCheckoutResponse> {
    return this.http.post<SubscriptionCheckoutResponse>(
      `${this.base}${API_ENDPOINTS.PAYMENTS.SUBSCRIPTIONS_CHECKOUT}`,
      body,
    );
  }

  /**
   * Full subscription payment flow: SelectPlan (creates a PendingPayment
   * subscription with the chosen plan) → checkout (Stripe session for that
   * subscription). The Stripe webhook activates the subscription on success.
   */
  startSubscriptionCheckout(opts: StartCheckoutOptions): Observable<SubscriptionCheckoutResponse> {
    return this.selectPlan({
      userId: opts.userId,
      companyId: opts.companyId ?? null,
      planId: opts.planId,
      billingCycle: opts.billingCycle,
    }).pipe(
      switchMap((sel) =>
        this.checkoutSubscription({
          userId: opts.userId,
          subscriptionId: sel.subscriptionId,
          planId: opts.planId,
          billingCycle: opts.billingCycle,
          successRedirectUrl: opts.successRedirectUrl,
          cancelRedirectUrl: opts.cancelRedirectUrl,
        }),
      ),
    );
  }
}
