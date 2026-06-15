import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { timeout } from 'rxjs/operators';
import { environment } from '../../../../../environments/environment';
import { AssignedTicket, TicketPayment } from '../models';

@Injectable({ providedIn: 'root' })
export class TicketsService {
  private http = inject(HttpClient);
  private base = environment.apiUrl;

  getMyAssignments(): Observable<AssignedTicket[]> {
    return this.http
      .get<AssignedTicket[]>(`${this.base}/maintenance/tickets/my-assignments`)
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

  setPrice(ticketId: string, amount: number, currency: string): Observable<TicketPayment> {
    return this.http.post<TicketPayment>(`${this.base}/maintenance/tickets/${ticketId}/payments`, {
      amount,
      currency,
    });
  }

  completeWork(ticketId: string): Observable<void> {
    return this.http.post<void>(`${this.base}/maintenance/tickets/${ticketId}/complete-work`, {});
  }

  getPayments(ticketId: string): Observable<TicketPayment[]> {
    return this.http.get<TicketPayment[]>(`${this.base}/maintenance/tickets/${ticketId}/payments`);
  }

  confirmPayment(paymentId: string): Observable<void> {
    return this.http.post<void>(
      `${this.base}/maintenance/tickets/payments/${paymentId}/confirm`,
      {},
    );
  }
}
