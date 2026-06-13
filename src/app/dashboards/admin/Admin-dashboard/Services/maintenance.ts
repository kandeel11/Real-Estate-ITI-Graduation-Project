import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, BehaviorSubject } from 'rxjs';
import { tap } from 'rxjs/operators';
import { environment } from '../../../../../environments/environment';
import {
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
}
