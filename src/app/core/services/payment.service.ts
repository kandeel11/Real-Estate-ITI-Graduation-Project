import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { API_ENDPOINTS } from '../constants/api-endpoints';
import {
  RentPayment,
  ProcessPaymentRequest,
  ProcessPaymentResponse,
  GenerateMonthlyResponse,
  PaymentSchedule,
} from '../models/payment.model';

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
}
