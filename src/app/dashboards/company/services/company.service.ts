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
import { environment } from '../../../../environments/environment';
import { API_ENDPOINTS } from '../../../core/constants/api-endpoints';
import {
  Company,
  CompanyStats,
  Broker,
  AddBrokerRequest,
  ActivityItem,
} from '../../../shared/models/company.model';

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
