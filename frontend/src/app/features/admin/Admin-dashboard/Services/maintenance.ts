import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, BehaviorSubject } from 'rxjs';
import { map, tap, timeout } from 'rxjs/operators';
import { environment } from '../../../../../environments/environment';
import {
  AdminTicket,
  CreateMaintenanceCategoryRequest,
  CreateMaintenanceCompanyRequest,
  MaintenanceCategory,
  MaintenanceCompany,
} from '../Models';

@Injectable({ providedIn: 'root' })
export class MaintenanceService {
  private http = inject(HttpClient);
  private base = environment.apiUrl;

  private categoriesSubject = new BehaviorSubject<MaintenanceCategory[]>([]);
  categories$ = this.categoriesSubject.asObservable();

  private companiesSubject = new BehaviorSubject<MaintenanceCompany[]>([]);
  companies$ = this.companiesSubject.asObservable();

  // ===== CATEGORIES =====
  getCategories(): Observable<MaintenanceCategory[]> {
    return this.http
      .get<MaintenanceCategory[]>(`${this.base}/maintenance-categories`)
      .pipe(tap((list) => this.categoriesSubject.next(list)));
  }

  createCategory(body: CreateMaintenanceCategoryRequest): Observable<MaintenanceCategory> {
    return this.http.post<MaintenanceCategory>(`${this.base}/maintenance-categories`, body).pipe(
      tap((created) => {
        this.categoriesSubject.next([...this.categoriesSubject.value, created]);
      }),
    );
  }

  deleteCategory(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/maintenance-categories/${id}`).pipe(
      tap(() => {
        this.categoriesSubject.next(this.categoriesSubject.value.filter((c) => c.id !== id));
      }),
    );
  }

  // ===== COMPANIES =====
  getCompanies(): Observable<MaintenanceCompany[]> {
    return this.http
      .get<MaintenanceCompany[]>(`${this.base}/maintenance-companies`)
      .pipe(tap((list) => this.companiesSubject.next(list)));
  }

  createCompany(body: CreateMaintenanceCompanyRequest): Observable<MaintenanceCompany> {
    return this.http.post<MaintenanceCompany>(`${this.base}/maintenance-companies`, body).pipe(
      tap((created) => {
        this.companiesSubject.next([...this.companiesSubject.value, created]);
      }),
    );
  }

  // ===== TICKETS =====
  getAllTickets(): Observable<AdminTicket[]> {
    return this.http.get<any>(`${this.base}/maintenance/tickets/admin`).pipe(
      timeout(15000),
      map((res) => Array.isArray(res) ? res : (res?.$values ?? res?.data ?? res?.items ?? [])),
    );
  }

  // ===== DISPUTES =====
  getDisputeDetails(ticketId: string): Observable<DisputeDetailsDto> {
    return this.http.get<DisputeDetailsDto>(
      `${this.base}/maintenance/tickets/${ticketId}/dispute-details`
    ).pipe(timeout(15000));
  }

  resolveDispute(ticketId: string, finalResponsibleParty: string, adminNotes: string | null): Observable<void> {
    return this.http.post<void>(
      `${this.base}/maintenance/tickets/${ticketId}/resolve-dispute`,
      { finalResponsibleParty, adminNotes }
    );
  }
}

// ── Dispute DTOs ─────────────────────────────────────────────────────────────
export interface LeaseContractSummaryDto {
  leaseId: string;
  monthlyInstallment: number | null;
  totalRentAmount: number | null;
  securityDeposit: number | null;
  startDate: string | null;
  endDate: string | null;
  durationInMonths: number | null;
  paymentFrequency: string | null;
  contractFileUrl: string | null;
  isFullManagement: boolean;
  managementOptions: string | null;
}

export interface DisputeDetailsDto {
  ticketId: string;
  ticketNumber: string | null;
  title: string;
  description: string;
  priority: string;
  status: string;
  propertyAddress: string | null;
  propertyId: string;
  tenantId: string;
  ownerId: string;
  createdAt: string;
  aiEvaluation: {
    id: string;
    responsibleParty: string;
    category: string;
    confidenceScore: number;
    reasoning: string;
    evaluatedAt: string;
  } | null;
  images: { id: string; fileUrl: string; fileType: string; fileSizeKb: number; uploadedAt: string }[];
  disputedById: string | null;
  disputedByRole: string | null;
  disputeReason: string | null;
  disputeDate: string | null;
  adminNotes: string | null;
  leaseContract: LeaseContractSummaryDto | null;
}
