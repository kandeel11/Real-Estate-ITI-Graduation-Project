import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, map, catchError, timeout } from 'rxjs';
import {
  BrokerProfile, BrokerStats, BrokerTask, BrokerActivity,
  Lead, Inquiry, PerformanceStats, Rating,
  ChatMessage, InquiryDetail, LeaseAiExtraction,
} from '../models/broker.models';
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

/**
 * All read endpoints are served by the backend BrokerDashboardController
 * (route: /api/brokers/me/*), scoped to the logged-in broker via JWT.
 * No mock data — empty arrays are legitimate "nothing yet" states.
 */
@Injectable({ providedIn: 'root' })
export class BrokerDataService {
  private readonly http = inject(HttpClient);
  private readonly api  = environment.apiUrl;
  private readonly me   = `${environment.apiUrl}/brokers/me`;

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
    // Handles both response shapes the backend may return:
    //   Shape A — { value, confidence } objects (snake_case or camelCase keys)
    //   Shape B — flat values (camelCase, original format)
    const mapStatus = (s?: string): LeaseAiExtraction['extractionStatus'] => {
      const low = (s ?? '').toLowerCase();
      if (low === 'extracted' || low === 'done') return 'done';
      if (low === 'failed') return 'failed';
      return 'pending';
    };
    // Extract string value from either a { value } object or a plain scalar.
    const v = (f: any): string => {
      if (f === null || f === undefined) return '';
      if (typeof f === 'object' && 'value' in f) return String(f.value ?? '');
      return String(f);
    };
    // Extract numeric value, stripping commas ("60,000" → 60000).
    const n = (f: any): number => {
      const s = v(f).replace(/,/g, '');
      return s ? Number(s) : 0;
    };
    // Extract per-field confidence from a { confidence } object (undefined if flat).
    const c = (f: any): number | undefined =>
      f && typeof f === 'object' && 'confidence' in f ? (f as any).confidence : undefined;

    return this.http.get<any>(`${this.api}/leases/${leaseId}/ai-extraction`).pipe(
      timeout(10000),
      map((r): LeaseAiExtraction => ({
        tenantName:        v(r.tenant_name       ?? r.tenantName),
        tenantEmail:       '',
        ownerName:         v(r.owner_name        ?? r.ownerName),
        propertyTitle:     '',
        startDate:         v(r.start_date        ?? r.startDate),
        endDate:           v(r.end_date          ?? r.endDate),
        installmentAmount: n(r.installment_amount ?? r.installmentAmount),
        paymentFrequency:  (v(r.payment_frequency ?? r.paymentFrequency) || 'Monthly') as LeaseAiExtraction['paymentFrequency'],
        totalRentAmount:   (r.total_rent_amount ?? r.totalRentAmount)
                             ? n(r.total_rent_amount ?? r.totalRentAmount) : null,
        durationInMonths:  (r.duration_in_months ?? r.durationInMonths)
                             ? n(r.duration_in_months ?? r.durationInMonths) : null,
        deposit:           n(r.security_deposit  ?? r.securityDeposit ?? r.deposit),
        paymentDayOfMonth: (r.payment_day_of_month ?? r.paymentDayOfMonth)
                             ? n(r.payment_day_of_month ?? r.paymentDayOfMonth) : null,
        extractionStatus:  mapStatus(r.extractionStatus ?? r.extraction_status),
        confidence:        r.overall_confidence ?? r.overallConfidence ?? r.confidence ?? r.extractionConfidence ?? 0,
        fieldConfidence: {
          tenantName:        c(r.tenant_name)        ?? c(r.tenantName),
          ownerName:         c(r.owner_name)         ?? c(r.ownerName),
          startDate:         c(r.start_date)         ?? c(r.startDate),
          endDate:           c(r.end_date)           ?? c(r.endDate),
          installmentAmount: c(r.installment_amount) ?? c(r.installmentAmount),
          paymentFrequency:  c(r.payment_frequency)  ?? c(r.paymentFrequency),
          totalRentAmount:   c(r.total_rent_amount)  ?? c(r.totalRentAmount),
          deposit:           c(r.security_deposit)   ?? c(r.securityDeposit),
          paymentDayOfMonth: c(r.payment_day_of_month) ?? c(r.paymentDayOfMonth),
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
    // Broker-scoped enriched leases (Lease.BrokerId == me), served by BrokerDashboardController.
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
}
