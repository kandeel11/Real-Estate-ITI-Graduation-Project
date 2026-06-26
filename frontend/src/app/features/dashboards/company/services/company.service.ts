/**
 * Company Service
 * Live against the backend (CompaniesController / BrokersController) — all
 * calls require a CompanyManager JWT. `useMock` remains as an offline demo
 * fallback. Stats and activity come from the composed Host endpoints
 * /companies/mine/stats and /companies/mine/activity.
 */

import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, throwError, delay } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { API_ENDPOINTS } from '../../../../core/constants/api-endpoints';
import {
  Company,
  CompanyStats,
  Broker,
  AddBrokerRequest,
  ActivityItem,
  RatingSummary,
  CompanyInquiry,
} from '../../../../shared/models/company.model';

const MOCK_COMPANY: Company = {
  id: 'c1',
  name: 'Acme Real Estate',
  commercialRegister: '12345',
  logoUrl: null,
  website: 'www.acme-eg.com',
  description: 'Cairo-based brokerage since 2015.',
  city: 'Cairo',
  address: '12 Tahrir St.',
  phone: '+20 2 1234 5678',
  email: 'contact@acme-eg.com',
  isVerified: true,
  ratingAvg: 4.7,
  totalRatings: 89,
};

const MOCK_STATS: CompanyStats = {
  totalProperties: 147,
  activeProperties: 140,
  pendingProperties: 2,
  rentedProperties: 5,
  totalBrokers: 23,
  totalViews: 3421,
  totalInquiries: 312,
  monthlyInquiries: 188,
  dealsClosed: 12,
};

const MOCK_ACTIVITY: ActivityItem[] = [
  { type: 'listing_created', text: 'Listing "Maadi 3BR" created', timestamp: '2026-06-11T08:00:00Z' },
  { type: 'inquiry_received', text: 'New inquiry received on "Maadi 3BR"', timestamp: '2026-06-11T06:00:00Z' },
  { type: 'broker_joined', text: 'Broker Layla Ibrahim joined the team', timestamp: '2026-06-08T10:00:00Z' },
];

const MOCK_BROKERS: Broker[] = [
  { id: 'b1', userId: 'u1', companyId: 'c1', fullName: 'Sara Hassan', email: 'sara@acme.com',
    phone: '+20 100 111 1111', licenseNumber: 'ABC123', isActive: true, aiPerformanceScore: 87,
    closedDealsCount: 12, avgResponseTimeMinutes: 4, ratingAvg: 4.9, totalRatings: 47 },
  { id: 'b2', userId: 'u2', companyId: 'c1', fullName: 'Mahmoud Ali', email: 'mahmoud@acme.com',
    phone: '+20 100 222 2222', licenseNumber: 'ABC124', isActive: true, aiPerformanceScore: 79,
    closedDealsCount: 8, avgResponseTimeMinutes: 11, ratingAvg: 4.7, totalRatings: 31 },
  { id: 'b3', userId: 'u3', companyId: 'c1', fullName: 'Layla Ibrahim', email: 'layla@acme.com',
    phone: '+20 100 333 3333', licenseNumber: 'ABC125', isActive: true, aiPerformanceScore: 82,
    closedDealsCount: 6, avgResponseTimeMinutes: 9, ratingAvg: 4.8, totalRatings: 28 },
  { id: 'b4', userId: 'u4', companyId: 'c1', fullName: 'Karim Said', email: 'karim@acme.com',
    phone: '+20 100 444 4444', licenseNumber: 'ABC126', isActive: false, aiPerformanceScore: 54,
    closedDealsCount: 4, avgResponseTimeMinutes: 47, ratingAvg: 4.1, totalRatings: 12 },
];

const MAX_BROKERS = 25;

// ── CRM Lead Pipeline ────────────────────────────────────────────────────────

export interface LeadPipelineItem {
  id: string;
  inquiryId: string;
  propertyId: string;
  tenantId: string;
  ownerId: string;
  status: string;
  dealValue: number | null;
  notes: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface BrokerPipeline {
  new: LeadPipelineItem[];
  contacted: LeadPipelineItem[];
  negotiating: LeadPipelineItem[];
  closedWon: LeadPipelineItem[];
  closedLost: LeadPipelineItem[];
}

export interface BrokerPipelineDto {
  brokerId: string;
  brokerUserId: string;
  brokerName: string;
  brokerEmail: string;
  totalLeads: number;
  pipeline: BrokerPipeline;
}

export interface CompanyLeadPipelineDto {
  companyId: string;
  companyName: string;
  totalBrokers: number;
  brokers: BrokerPipelineDto[];
}

// ── Pending lease approval ───────────────────────────────────────────────────

export interface PendingLeaseApproval {
  id: string;
  propertyId: string;
  propertyTitle: string;
  tenantId: string;
  tenantName: string;
  brokerId: string | null;
  brokerName: string | null;
  installmentAmount: number | null;
  paymentFrequency: string | null;
  totalRentAmount: number | null;
  durationInMonths: number | null;
  securityDeposit: number | null;
  startDate: string | null;
  endDate: string | null;
  isFullManagement: boolean;
  managementOptions: string[];
  uploadedAt: string;
  contractFileUrl: string | null;
}

/** Deal closed by broker without management, waiting for manager confirmation. */
export interface NoManagementPendingItem {
  leadId: string;
  propertyId: string;
  tenantId: string;
  tenantName: string;
  dealValue: number | null;
  pendingAt: string;
}

/** Lease the manager already approved — now waiting for the tenant to accept. */
export interface AwaitingTenantLease {
  id: string;
  propertyTitle: string;
  tenantName: string;
  brokerName: string | null;
  installmentAmount: number | null;
  paymentFrequency: string | null;
  startDate: string | null;
  endDate: string | null;
  managementOptions: string[];
  approvedAt: string;
  uploadedAt: string;
  contractFileUrl: string | null;
}

/** Lease that is currently active (tenant accepted). */
export interface ActiveCompanyLease {
  id: string;
  propertyId: string;
  propertyTitle: string;
  tenantId: string;
  tenantName: string;
  brokerId: string | null;
  brokerName: string | null;
  installmentAmount: number | null;
  paymentFrequency: string | null;
  totalRentAmount: number | null;
  durationInMonths: number | null;
  securityDeposit: number | null;
  startDate: string | null;
  endDate: string | null;
  isFullManagement: boolean;
  managementOptions: string[];
  activeSince: string | null;
  uploadedAt: string;
}

@Injectable({ providedIn: 'root' })
export class CompanyService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;
  private useMock = false; // ← offline demo fallback; backend is live

  /** GET /api/companies/mine — company of the logged-in manager. */
  getMyCompany(): Observable<Company> {
    if (this.useMock) return of(MOCK_COMPANY).pipe(delay(300));
    return this.http.get<Company>(`${this.baseUrl}${API_ENDPOINTS.COMPANIES.MINE}`);
  }

  /** GET /api/companies/mine/stats — composed dashboard counters. */
  getStats(): Observable<CompanyStats> {
    if (this.useMock) return of(MOCK_STATS).pipe(delay(400));
    return this.http.get<CompanyStats>(
      `${this.baseUrl}${API_ENDPOINTS.COMPANIES.MINE_STATS}`
    );
  }

  /** GET /api/companies/mine/activity — recent listings/brokers/inquiries. */
  getActivity(): Observable<ActivityItem[]> {
    if (this.useMock) return of([...MOCK_ACTIVITY]).pipe(delay(350));
    return this.http.get<ActivityItem[]>(
      `${this.baseUrl}${API_ENDPOINTS.COMPANIES.MINE_ACTIVITY}`
    );
  }

  /** GET /api/companies/mine/inquiries — full inquiry history (tenant + broker enriched). */
  getInquiries(): Observable<CompanyInquiry[]> {
    if (this.useMock) return of([] as CompanyInquiry[]).pipe(delay(300));
    return this.http.get<CompanyInquiry[]>(
      `${this.baseUrl}${API_ENDPOINTS.COMPANIES.MINE_INQUIRIES}`
    );
  }

  /** GET /api/companies/mine/ratings-summary — star distribution. */
  getRatingsSummary(): Observable<RatingSummary> {
    return this.http.get<RatingSummary>(
      `${this.baseUrl}${API_ENDPOINTS.COMPANIES.MINE_RATINGS_SUMMARY}`
    );
  }

  /** POST /api/companies/mine/logo — uploads and persists the logo, returns its URL. */
  uploadLogo(file: File): Observable<{ logoUrl: string }> {
    const form = new FormData();
    form.append('File', file);
    return this.http.post<{ logoUrl: string }>(
      `${this.baseUrl}${API_ENDPOINTS.COMPANIES.MINE_LOGO}`,
      form
    );
  }

  /** GET /api/companies/mine/brokers */
  getBrokers(): Observable<Broker[]> {
    if (this.useMock) return of([...MOCK_BROKERS]).pipe(delay(350));
    return this.http.get<Broker[]>(
      `${this.baseUrl}${API_ENDPOINTS.COMPANIES.MINE_BROKERS}`
    );
  }

  /** POST /api/companies/mine/brokers */
  addBroker(req: AddBrokerRequest): Observable<{ brokerId: string }> {
    if (this.useMock) {
      // Simulate the backend's subscription limit check
      if (MOCK_BROKERS.filter((b) => b.isActive).length >= MAX_BROKERS) {
        return throwError(() => ({
          error: { error: `Broker limit of ${MAX_BROKERS} reached. Upgrade your plan to add more.` },
        }));
      }
      const broker: Broker = {
        id: 'b' + Date.now(),
        userId: 'u' + Date.now(),
        companyId: 'c1',
        fullName: req.fullName,
        email: req.email,
        phone: req.phone,
        licenseNumber: req.licenseNumber,
        isActive: true,
        aiPerformanceScore: 0,
        closedDealsCount: 0,
        avgResponseTimeMinutes: 0,
        ratingAvg: 0,
        totalRatings: 0,
      };
      MOCK_BROKERS.push(broker);
      return of({ brokerId: broker.id }).pipe(delay(600));
    }
    return this.http.post<{ brokerId: string }>(
      `${this.baseUrl}${API_ENDPOINTS.BROKERS.ADD_TO_MINE}`,
      req
    );
  }

  /** DELETE /api/companies/mine/brokers/{id} — also blocks the broker's login. */
  deactivateBroker(brokerId: string): Observable<void> {
    if (this.useMock) {
      const broker = MOCK_BROKERS.find((b) => b.id === brokerId);
      if (broker) broker.isActive = false;
      return of(void 0).pipe(delay(300));
    }
    return this.http.delete<void>(
      `${this.baseUrl}${API_ENDPOINTS.BROKERS.DEACTIVATE_MINE(brokerId)}`
    );
  }

  /** POST /api/companies/mine/brokers/{id}/reactivate — re-checks the plan's broker limit. */
  reactivateBroker(brokerId: string): Observable<void> {
    if (this.useMock) {
      const broker = MOCK_BROKERS.find((b) => b.id === brokerId);
      if (broker) broker.isActive = true;
      return of(void 0).pipe(delay(300));
    }
    return this.http.post<void>(
      `${this.baseUrl}${API_ENDPOINTS.BROKERS.REACTIVATE_MINE(brokerId)}`,
      {}
    );
  }

  /**
   * DELETE /api/companies/mine/brokers/{id}/permanent — removes the broker
   * and blocks their login. Their properties stay and can be reassigned via
   * the Properties page.
   */
  deleteBroker(brokerId: string): Observable<void> {
    if (this.useMock) {
      const idx = MOCK_BROKERS.findIndex((b) => b.id === brokerId);
      if (idx >= 0) MOCK_BROKERS.splice(idx, 1);
      return of(void 0).pipe(delay(300));
    }
    return this.http.delete<void>(
      `${this.baseUrl}${API_ENDPOINTS.BROKERS.DELETE_MINE(brokerId)}`
    );
  }

  /** GET /api/companies/mine/lead-pipeline — all brokers' leads bucketed by status. */
  getLeadPipeline(): Observable<CompanyLeadPipelineDto> {
    return this.http.get<CompanyLeadPipelineDto>(`${this.baseUrl}/companies/mine/lead-pipeline`);
  }

  /** GET /api/companies/mine/leases/pending-approval — leases awaiting manager sign-off. */
  getPendingApprovals(): Observable<PendingLeaseApproval[]> {
    return this.http.get<PendingLeaseApproval[]>(
      `${this.baseUrl}/companies/mine/leases/pending-approval`,
    );
  }

  /** GET /api/companies/mine/leases/awaiting-tenant — approved by manager, pending tenant accept. */
  getAwaitingTenantLeases(): Observable<AwaitingTenantLease[]> {
    return this.http.get<AwaitingTenantLease[]>(
      `${this.baseUrl}/companies/mine/leases/awaiting-tenant`,
    );
  }

  /** GET /api/companies/mine/leases/active — tenant-accepted active leases. */
  getActiveLeases(): Observable<ActiveCompanyLease[]> {
    return this.http.get<ActiveCompanyLease[]>(
      `${this.baseUrl}/companies/mine/leases/active`,
    );
  }

  /** GET /api/leases/{id}/download — returns a time-limited SAS URL for the uploaded PDF. */
  downloadLeaseContract(leaseId: string): Observable<{ contractFileUrl: string }> {
    return this.http.get<{ contractFileUrl: string }>(`${this.baseUrl}/leases/${leaseId}/download`);
  }

  /** POST /api/leases/{id}/manager-approve — approve or reject a partial-management lease. */
  approveLease(leaseId: string, isApproved: boolean, notes?: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/leases/${leaseId}/manager-approve`, {
      isApproved,
      notes: notes ?? null,
    });
  }

  /** GET /api/companies/mine/leads/no-management-pending — broker closed deals awaiting manager confirmation. */
  getNoManagementPending(): Observable<NoManagementPendingItem[]> {
    return this.http.get<NoManagementPendingItem[]>(
      `${this.baseUrl}/companies/mine/leads/no-management-pending`,
    );
  }

  /** POST /api/companies/mine/leads/{leadId}/confirm-no-management — manager confirms the no-management close. */
  confirmNoManagement(leadId: string): Observable<void> {
    return this.http.post<void>(
      `${this.baseUrl}/companies/mine/leads/${leadId}/confirm-no-management`,
      {},
    );
  }

  /** PUT /api/companies/mine — backend returns 204 No Content. */
  updateCompany(changes: Partial<Company>): Observable<void> {
    if (this.useMock) {
      Object.assign(MOCK_COMPANY, changes);
      return of(void 0).pipe(delay(400));
    }
    return this.http.put<void>(
      `${this.baseUrl}${API_ENDPOINTS.COMPANIES.UPDATE_MINE}`,
      changes
    );
  }
}
