import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../../environments/environment';

// ── Status / Priority ────────────────────────────────────────────────────────
export type TicketStatus =
  | 'Draft'
  | 'Submitted'
  | 'UnderAiReview'
  | 'AiEvaluated'
  | 'WaitingForOwnerDecision'
  | 'WaitingForTenantDecision'
  | 'AssignedToVendor'
  | 'VendorAccepted'
  | 'InProgress'
  | 'AwaitingPayment'
  | 'Completed'
  | 'Cancelled'
  | 'Rejected';

export type TicketPriority = 'Low' | 'Medium' | 'Emergency';
export type ResolutionMethod = 0 | 1; // 0 = PlatformVendor, 1 = ExternalTechnician

// ── List / Paginated ─────────────────────────────────────────────────────────
export interface TicketListItem {
  id: string;
  ticketNumber: string;
  title: string;
  priority: TicketPriority;
  status: TicketStatus;
  propertyAddress: string;
  attachmentCount: number;
  createdAt: string;
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

// ── Sub-models ───────────────────────────────────────────────────────────────
export interface AiEvaluation {
  id: string;
  responsibleParty: 'Owner' | 'Tenant';
  category: string;
  confidenceScore: number;
  reasoning: string;
  evaluatedAt: string;
}

export interface VendorAssignment {
  id: string;
  vendorId: string;
  status: string;
  rejectionReason: string | null;
  acceptedAt: string | null;
  createdAt: string;
}

export interface TicketAttachment {
  id: string;
  fileUrl: string;
  fileType: string;
  fileSizeKb: number;
  uploadedAt: string;
}

export interface TicketComment {
  id: string;
  userId: string;
  userRole: string;
  content: string;
  isInternal: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface StatusHistoryItem {
  fromStatus: string;
  toStatus: string;
  changedBy: string;
  notes: string | null;
  changedAt: string;
}

export interface TicketPayment {
  id: string;
  amount: number;
  currency: string;
  status: string;
  paymentMethod: string | null;
  transactionId: string | null;
  paidAt: string | null;
  createdAt: string;
  /** Platform commission (2% of amount). */
  commissionAmount?: number | null;
  /** Net amount paid to the vendor (98% of amount). */
  netAmount?: number | null;
}

// ── Full detail ──────────────────────────────────────────────────────────────
export interface TicketDetail {
  id: string;
  ticketNumber: string;
  propertyId: string;
  propertyAddress: string;
  tenantId: string;
  tenantEmail: string;
  tenantPhone: string;
  ownerId: string;
  title: string;
  description: string;
  priority: TicketPriority;
  status: TicketStatus;
  responsibleParty: string | null;
  resolutionMethod: ResolutionMethod | 'PlatformVendor' | 'ExternalTechnician' | null;
  aiEvaluation: AiEvaluation | null;
  vendorAssignment: VendorAssignment | null;
  attachments: TicketAttachment[];
  comments: TicketComment[];
  statusHistory: StatusHistoryItem[];
  payment: TicketPayment | null;
  createdAt: string;
  updatedAt: string;
  resolvedAt: string | null;
  /** Set when the AI pipeline routed this ticket to a management company. */
  routedToCompanyId?: string | null;
  /** On company-shadow tickets: ID of the original tenant ticket. */
  parentTicketId?: string | null;
}

// ── Request bodies ───────────────────────────────────────────────────────────
export interface CreateTicketRequest {
  propertyId: string;
  ownerId: string;
  leaseId: string | null;
  title: string;
  description: string;
  priority: TicketPriority;
}

export interface SubmitTicketRequest {
  propertyAddress: string;
  tenantPhone: string;
  tenantEmail: string;
}

// ── Service ──────────────────────────────────────────────────────────────────
@Injectable({ providedIn: 'root' })
export class MaintenanceService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  // 1. All my tickets (paginated + filtered)
  getMyTickets(status = '', page = 1, pageSize = 10): Observable<PagedResult<TicketListItem>> {
    const params: Record<string, string | number> = { page, pageSize };
    if (status) params['status'] = status;
    return this.http.get<PagedResult<TicketListItem>>(`${this.base}/maintenance/tickets`, { params });
  }

  // 2. Full ticket detail
  getTicketDetail(id: string): Observable<TicketDetail> {
    return this.http.get<TicketDetail>(`${this.base}/maintenance/tickets/${id}`);
  }

  // 3. Create ticket (status = Draft)
  createTicket(data: CreateTicketRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.base}/maintenance/tickets`, data);
  }

  // 4. Update ticket (Draft or Submitted)
  updateTicket(id: string, data: { title?: string; description?: string; priority?: string }): Observable<void> {
    return this.http.patch<void>(`${this.base}/maintenance/tickets/${id}`, { ticketId: id, ...data });
  }

  // 5. Delete ticket (Draft only)
  deleteTicket(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/maintenance/tickets/${id}`);
  }

  // 6. Upload image attachment
  uploadAttachment(ticketId: string, file: File): Observable<{ attachmentId: string }> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<{ attachmentId: string }>(
      `${this.base}/maintenance/tickets/${ticketId}/attachments`, form
    );
  }

  // 7. Submit ticket (triggers AI evaluation)
  submitTicket(id: string, data: SubmitTicketRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.base}/maintenance/tickets/${id}/submit`, data);
  }

  // 8. Get AI evaluation result
  getAiEvaluation(id: string): Observable<AiEvaluation> {
    return this.http.get<AiEvaluation>(`${this.base}/maintenance/tickets/${id}/ai-evaluation`);
  }

  // 9. Tenant decision (0 = PlatformVendor, 1 = ExternalTechnician)
  makeDecision(id: string, resolutionMethod: ResolutionMethod): Observable<void> {
    return this.http.post<void>(`${this.base}/maintenance/tickets/${id}/decision`, { resolutionMethod });
  }

  // 10. Cancel ticket
  cancelTicket(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/maintenance/tickets/${id}/cancel`, {});
  }

  // Mark ticket as complete (external technician flow)
  completeTicket(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/maintenance/tickets/${id}/complete`, {});
  }

  // 11. Add comment (always non-internal for tenant)
  addComment(id: string, content: string): Observable<TicketComment> {
    return this.http.post<TicketComment>(
      `${this.base}/maintenance/tickets/${id}/comments`,
      { content, isInternal: false }
    );
  }

  // 12. Pay online (AwaitingPayment)
  payTicket(paymentId: string, transactionId: string): Observable<void> {
    return this.http.post<void>(
      `${this.base}/maintenance/tickets/payments/${paymentId}/pay`,
      { transactionId }
    );
  }

  // 13. Get payment info
  getPayment(ticketId: string): Observable<TicketPayment> {
    return this.http.get<TicketPayment>(`${this.base}/maintenance/tickets/${ticketId}/payments`);
  }

  // 14. Create Stripe Checkout session for maintenance payment (redirect flow)
  createMaintenanceCheckout(ticketId: string): Observable<{ checkoutUrl: string }> {
    return this.http.post<{ checkoutUrl: string }>(
      `${this.base}/maintenance/tickets/${ticketId}/payments/checkout`, {}
    );
  }

  // 15. Company manager: tickets routed here by the AI pipeline
  getRoutedTickets(companyId: string): Observable<TicketDetail[]> {
    return this.http.get<TicketDetail[]>(
      `${this.base}/maintenance/tickets/company/${companyId}/routed`
    );
  }
}
