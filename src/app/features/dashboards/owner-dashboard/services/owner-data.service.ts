import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, map, catchError, timeout } from 'rxjs';
import {
  BrokerProfile, BrokerStats, BrokerTask, BrokerActivity,
  Lead, Inquiry, PerformanceStats, Rating,
  ChatMessage, InquiryDetail, LeaseAiExtraction,
} from '../models/owner.models';
import { Property } from '../../../../features/properties/models/property.model';
import { environment } from '../../../../../environments/environment';
import { API_ENDPOINTS } from '../../../../core/constants/api-endpoints';

export interface BrokerLease {
  id: string;
  propertyId: string;
  propertyTitle: string;
  tenantName: string;
  tenantEmail: string;
  startDate: string;
  endDate: string;
  installmentAmount: number;
  totalRentAmount?: number | null;
  paymentFrequency?: string | null;
  durationInMonths?: number | null;
  securityDeposit?: number | null;
  status: 'active' | 'ended' | 'expiring';
  contractFileUrl?: string;
  extractionStatus?: string;
  tenantStatus?: 'pending_tenant' | 'accepted' | 'declined';
  // ── Renewal / termination lifecycle (from backend LeaseDto.Status) ───────────
  lifecycleStatus?: LeaseLifecycleStatus;
  renewalRequestedAt?: string | null;
  /** A renewal contract has been uploaded but not yet sent to the tenant — offer "Resume". */
  renewalInProgress?: boolean;
  terminationRequestedAt?: string | null;
  terminationRequestedByTenant?: boolean;
}

export type LeaseLifecycleStatus =
  | 'pending'
  | 'active'
  | 'renewal_requested'
  | 'termination_requested'
  | 'terminated'
  | 'ended';

/** Maintenance ticket as returned by GET /api/maintenance/tickets (owner-scoped via JWT). */
export interface MaintenanceTicket {
  id: string;
  ticketNumber: string;
  title: string;
  priority: string;
  status: string;
  propertyAddress: string | null;
  attachmentCount: number;
  createdAt: string;
}

interface PagedTickets {
  items: MaintenanceTicket[];
  total?: number;
  totalCount?: number;
}

/**
 * All read endpoints are served by the backend OwnerDashboardController
 * (route: /api/owners/me/*), scoped to the logged-in owner via JWT.
 * No mock data — empty arrays are legitimate "nothing yet" states.
 */
@Injectable({ providedIn: 'root' })
export class OwnerDataService {
  private readonly http = inject(HttpClient);
  private readonly api  = environment.apiUrl;
  private readonly me   = `${environment.apiUrl}/owners/me`;

  // ── Overview ─────────────────────────────────────────────────────────────────

  getProfile(): Observable<BrokerProfile> {
    return this.http.get<BrokerProfile>(this.me).pipe(timeout(10000));
  }

  getStats(): Observable<BrokerStats> {
    return this.http.get<BrokerStats>(`${this.me}/stats`).pipe(timeout(10000));
  }

  getTasks(): Observable<BrokerTask[]> {
    return this.http.get<BrokerTask[]>(`${this.me}/tasks`)
      .pipe(timeout(10000), catchError(() => of([])));
  }

  getRecentActivity(): Observable<BrokerActivity[]> {
    return this.http.get<BrokerActivity[]>(`${this.me}/activity`)
      .pipe(timeout(10000), catchError(() => of([])));
  }

  getPerformance(): Observable<PerformanceStats> {
    return this.http.get<PerformanceStats>(`${this.me}/performance`).pipe(timeout(10000));
  }

  getRatings(): Observable<Rating[]> {
    return this.http.get<Rating[]>(`${this.me}/ratings`)
      .pipe(timeout(10000), catchError(() => of([])));
  }

  // ── Properties ───────────────────────────────────────────────────────────────

  getProperties(): Observable<Property[]> {
    return this.http.get<Property[]>(`${this.me}/properties`)
      .pipe(timeout(10000), catchError(() => of([])));
  }

  // ── Inquiries ────────────────────────────────────────────────────────────────

  /** Compact list shape used by the Inquiries page. */
  getInquiries(): Observable<Inquiry[]> {
    return this.http.get<InquiryDetail[]>(`${this.me}/inquiries`).pipe(
      timeout(10000),
      map(list => list.map(this.toInquiry)),
      catchError(() => of([])),
    );
  }

  /** Full inquiry detail list (used by the chat modal, which needs the richer shape). */
  getInquiryDetails(): Observable<InquiryDetail[]> {
    return this.http.get<InquiryDetail[]>(`${this.me}/inquiries`)
      .pipe(timeout(10000), catchError(() => of([])));
  }

  getInquiriesByProperty(propertyId: string): Observable<InquiryDetail[]> {
    return this.http.get<InquiryDetail[]>(`${this.me}/inquiries`).pipe(
      timeout(10000),
      map(list => list.filter(i => i.propertyId === propertyId)),
      catchError(() => of([])),
    );
  }

  private toInquiry(i: InquiryDetail): Inquiry {
    return {
      id: i.id,
      from: i.tenantName,
      property: i.propertyTitle,
      message: i.message,
      status: i.status === 'new' ? 'new' : 'replied',
      timeAgo: i.createdAt,
    };
  }

  replyToInquiry(inquiryId: string, text: string): Observable<void> {
    return this.http.post<void>(`${this.api}/inquiries/${inquiryId}/reply`, { text });
  }

  convertToLead(inquiryId: string): Observable<{ leadId: string }> {
    return this.http.post<{ leadId: string }>(`${this.api}/inquiries/${inquiryId}/convert-to-lead`, {});
  }

  // ── Messages (conversation thread per inquiry) ───────────────────────────────

  getMessages(inquiryId: string): Observable<ChatMessage[]> {
    return this.http.get<ChatMessage[]>(`${this.api}/inquiries/${inquiryId}/messages`)
      .pipe(timeout(10000), catchError(() => of([])));
  }

  sendMessage(inquiryId: string, text: string): Observable<ChatMessage> {
    return this.http.post<ChatMessage>(`${this.api}/inquiries/${inquiryId}/messages`, { text });
  }

  // ── Leads ────────────────────────────────────────────────────────────────────

  getLeads(): Observable<Lead[]> {
    return this.http.get<Lead[]>(`${this.me}/leads`)
      .pipe(timeout(10000), catchError(() => of([])));
  }

  updateLeadStatus(leadId: string, status: string): Observable<void> {
    // Backend validates lowercase snake-case status names; send the raw kanban status.
    return this.http.put<void>(`${this.api}/leads/${leadId}/status`, { newStatus: status });
  }

  closeLead(leadId: string, outcome: 'won' | 'lost'): Observable<void> {
    const newStatus = outcome === 'won' ? 'closed_won' : 'closed_lost';
    return this.http.put<void>(`${this.api}/leads/${leadId}/status`, { newStatus });
  }

  // ── Lease Upload / AI Flow ───────────────────────────────────────────────────

  uploadLeasePdf(leadId: string, tenantEmail: string, file: File): Observable<{ leaseId: string }> {
    // Backend LeasesController.Upload binds a `leadId` form field and derives the owner from the lead.
    const fd = new FormData();
    fd.append('leadId', leadId);
    fd.append('tenantEmail', tenantEmail);
    fd.append('file', file);
    return this.http.post<{ leaseId: string }>(`${this.api}/leases/upload`, fd);
  }

  getLeaseAiExtraction(leaseId: string): Observable<LeaseAiExtraction> {
    interface Field { value: string; confidence: number; }
    interface Raw {
      installment_amount?: Field; payment_frequency?: Field; total_rent_amount?: Field;
      duration_in_months?: Field; security_deposit?: Field; start_date?: Field; end_date?: Field;
      payment_day_of_month?: Field; tenant_name?: Field; owner_name?: Field;
      overall_confidence?: number; confidence?: number;
      extractionStatus?: string; extraction_status?: string;
    }
    const mapStatus = (s?: string): LeaseAiExtraction['extractionStatus'] => {
      if (!s) return 'pending';
      const low = s.toLowerCase();
      if (low === 'extracted' || low === 'done') return 'done';
      if (low === 'failed') return 'failed';
      return 'pending';
    };
    return this.http.get<Raw>(`${this.api}/leases/${leaseId}/ai-extraction`).pipe(
      timeout(10000),
      map((r): LeaseAiExtraction => ({
        tenantName: r.tenant_name?.value ?? '',
        tenantEmail: '',
        ownerName: r.owner_name?.value ?? '',
        propertyTitle: '',
        startDate: r.start_date?.value ?? '',
        endDate: r.end_date?.value ?? '',
        installmentAmount: Number(r.installment_amount?.value ?? 0),
        paymentFrequency: (r.payment_frequency?.value ?? 'Monthly') as LeaseAiExtraction['paymentFrequency'],
        totalRentAmount: r.total_rent_amount ? Number(r.total_rent_amount.value) : null,
        durationInMonths: r.duration_in_months ? Number(r.duration_in_months.value) : null,
        deposit: Number(r.security_deposit?.value ?? 0),
        paymentDayOfMonth: r.payment_day_of_month ? Number(r.payment_day_of_month.value) : null,
        extractionStatus: mapStatus(r.extractionStatus ?? r.extraction_status),
        confidence: r.overall_confidence ?? r.confidence ?? 0,
        fieldConfidence: {
          tenantName: r.tenant_name?.confidence,
          ownerName: r.owner_name?.confidence,
          startDate: r.start_date?.confidence,
          endDate: r.end_date?.confidence,
          installmentAmount: r.installment_amount?.confidence,
          paymentFrequency: r.payment_frequency?.confidence,
          totalRentAmount: r.total_rent_amount?.confidence,
          deposit: r.security_deposit?.confidence,
          paymentDayOfMonth: r.payment_day_of_month?.confidence,
        },
      })),
    );
  }

  getLeaseExtractionDebug(leaseId: string): Observable<{
    leaseId: string; extractionStatus: string;
    displayStatus: 'pending' | 'done' | 'failed';
    confidence: number; isReadyForFrontend: boolean;
    nullFields: string[]; diagnosis: string;
  }> {
    return this.http
      .get<any>(`${this.api}/leases/${leaseId}/extraction-debug`)
      .pipe(timeout(10000));
  }

  sendLeaseToTenant(leaseId: string, data: Partial<LeaseAiExtraction>): Observable<void> {
    return this.http.post<void>(`${this.api}/leases/${leaseId}/send-to-tenant`, data);
  }

  markPropertyRented(propertyId: string): Observable<void> {
    return this.http.patch<void>(`${this.api}${API_ENDPOINTS.PROPERTIES.MARK_RENTED(propertyId)}`, {});
  }

  // ── Leases List ──────────────────────────────────────────────────────────────

  getLeases(): Observable<BrokerLease[]> {
    return this.http.get<BrokerLease[]>(`${this.me}/leases`)
      .pipe(timeout(10000), catchError(() => of([])));
  }

  getContractUrl(leaseId: string): Observable<{ contractFileUrl: string }> {
    return this.http.get<{ contractFileUrl: string }>(`${this.api}/leases/${leaseId}/download`);
  }

  // ── Renewal / termination (landlord side) ────────────────────────────────────

  /** Upload a fresh contract to renew an existing lease — same AI cycle as a new upload. */
  renewLeaseUpload(leaseId: string, file: File): Observable<{ leaseId: string }> {
    const fd = new FormData();
    fd.append('file', file);
    return this.http.post<{ leaseId: string }>(`${this.api}/leases/${leaseId}/renew-upload`, fd);
  }

  requestTermination(leaseId: string, reason: string, effectiveDate?: string): Observable<void> {
    return this.http.post<void>(`${this.api}/leases/${leaseId}/terminate-request`, {
      reason,
      effectiveDate: effectiveDate ?? null,
    });
  }

  respondTermination(leaseId: string, approve: boolean, note?: string): Observable<void> {
    const action = approve ? 'terminate-approve' : 'terminate-reject';
    return this.http.post<void>(`${this.api}/leases/${leaseId}/${action}`, { note: note ?? null });
  }

  // ── Maintenance Tickets ──────────────────────────────────────────────────────

  getMaintenanceTickets(status = '', page = 1, pageSize = 10): Observable<{ items: MaintenanceTicket[]; total: number }> {
    const params: Record<string, string | number> = { page, pageSize };
    if (status) params['status'] = status;
    return this.http
      .get<PagedTickets>(`${this.api}/maintenance/tickets`, { params })
      .pipe(
        timeout(10000),
        map(res => ({ items: res?.items ?? [], total: res?.total ?? res?.totalCount ?? 0 })),
        catchError(() => of({ items: [], total: 0 })),
      );
  }
}
