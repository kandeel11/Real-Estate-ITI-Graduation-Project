import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { timeout } from 'rxjs/operators';
import { environment } from '../../../../../environments/environment';
import { TicketDetailDto } from '../models';

@Injectable({ providedIn: 'root' })
export class TicketsService {
  private http = inject(HttpClient);
  private base = environment.apiUrl;

  /** Active tickets only — pending + accepted, not rejected (used by Overview) */
  getMyAssignments(): Observable<TicketDetailDto[]> {
    return this.http
      .get<TicketDetailDto[]>(`${this.base}/maintenance/tickets/my-assignments`)
      .pipe(timeout(15000));
  }

  /** All tickets ever assigned, including history/completed/rejected (used by Tickets page) */
  getVendorDashboard(): Observable<TicketDetailDto[]> {
    return this.http
      .get<TicketDetailDto[]>(`${this.base}/maintenance/tickets/vendor-dashboard`)
      .pipe(timeout(15000));
  }

  /** Single ticket detail */
  getTicketById(id: string): Observable<TicketDetailDto> {
    return this.http
      .get<TicketDetailDto>(`${this.base}/maintenance/tickets/${id}`)
      .pipe(timeout(15000));
  }

  acceptAssignment(assignmentId: string): Observable<void> {
    return this.http.post<void>(
      `${this.base}/maintenance/tickets/assignments/${assignmentId}/accept`,
      {},
    );
  }

  rejectAssignment(assignmentId: string, reason: string): Observable<void> {
    return this.http.post<void>(
      `${this.base}/maintenance/tickets/assignments/${assignmentId}/reject`,
      { reason },
    );
  }

  startWork(ticketId: string): Observable<void> {
    return this.http.post<void>(`${this.base}/maintenance/tickets/${ticketId}/start-work`, {});
  }

  setPrice(ticketId: string, amount: number, currency: string): Observable<TicketDetailDto> {
    return this.http.post<TicketDetailDto>(
      `${this.base}/maintenance/tickets/${ticketId}/payments`,
      { amount, currency },
    );
  }

  completeWork(ticketId: string): Observable<void> {
    return this.http.post<void>(`${this.base}/maintenance/tickets/${ticketId}/complete-work`, {});
  }

  /**
   * Confirm payment received.
   * Only call when payment.transactionId is already set (payer submitted payment).
   */
  confirmPayment(paymentId: string, transactionId: string): Observable<void> {
    return this.http.post<void>(
      `${this.base}/maintenance/tickets/payments/${paymentId}/confirm`,
      { transactionId },
    );
  }
}
