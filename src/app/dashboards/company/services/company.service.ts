/**
 * Company Service
 * Live against the backend (CompaniesController / BrokersController) — all
 * calls require a CompanyManager JWT. `useMock` remains as an offline demo
 * fallback. Company stats have NO backend endpoint yet, so in live mode they
 * are DERIVED from /companies/mine/properties + /companies/mine/brokers;
 * inquiries and deals-closed stay 0 until a real stats endpoint exists.
 */

import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, throwError, delay, forkJoin, map } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { API_ENDPOINTS } from '../../../core/constants/api-endpoints';
import {
  Company,
  CompanyStats,
  Broker,
  AddBrokerRequest,
} from '../../../shared/models/company.model';
import { PropertyService } from '../../../features/properties/services/property.service';

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
  totalBrokers: 23,
  monthlyViews: 3421,
  monthlyInquiries: 188,
  monthlyDealsClosed: 12,
};

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

@Injectable({ providedIn: 'root' })
export class CompanyService {
  private http = inject(HttpClient);
  private propertySvc = inject(PropertyService);
  private baseUrl = environment.apiUrl;
  private useMock = false; // ← offline demo fallback; backend is live

  /** GET /api/companies/mine — company of the logged-in manager. */
  getMyCompany(): Observable<Company> {
    if (this.useMock) return of(MOCK_COMPANY).pipe(delay(300));
    return this.http.get<Company>(`${this.baseUrl}${API_ENDPOINTS.COMPANIES.MINE}`);
  }

  /**
   * No backend stats endpoint yet — derived from real properties + brokers.
   * monthlyInquiries / monthlyDealsClosed need a real endpoint and stay 0.
   */
  getStats(): Observable<CompanyStats> {
    if (this.useMock) return of(MOCK_STATS).pipe(delay(400));
    return forkJoin({
      properties: this.propertySvc.getCompanyProperties(),
      brokers: this.getBrokers(),
    }).pipe(
      map(({ properties, brokers }) => ({
        totalProperties: properties.length,
        activeProperties: properties.filter((p) => p.status === 'Active').length,
        pendingProperties: properties.filter((p) => p.status === 'PendingReview').length,
        totalBrokers: brokers.filter((b) => b.isActive).length,
        monthlyViews: properties.reduce((sum, p) => sum + (p.viewsCount ?? 0), 0),
        monthlyInquiries: 0,
        monthlyDealsClosed: 0,
      }))
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
