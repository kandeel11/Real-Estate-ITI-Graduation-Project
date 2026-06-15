import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, BehaviorSubject } from 'rxjs';
import { tap } from 'rxjs/operators';
import { environment } from '../../../../../environments/environment';
import {
  PendingProperty,
  Property,
  verificationStatus,
  UpdateVerificationRequest,
  PropertyDetail,
  LOCKED_STATUSES,
} from '../Models';

@Injectable({ providedIn: 'root' })
export class PropertyService {
  private http = inject(HttpClient);
  private base = environment.apiUrl;

  private propertiesSubject = new BehaviorSubject<Property[]>([]);
  properties$ = this.propertiesSubject.asObservable();

  getAllProperties(): Observable<Property[]> {
    return this.http
      .get<Property[]>(`${this.base}/properties`)
      .pipe(tap((list) => this.propertiesSubject.next(list)));
  }
  getPropertyById(id: string): Observable<PropertyDetail> {
    return this.http.get<PropertyDetail>(`${this.base}/properties/${id}`);
  }
  getPendingProperties(): Observable<PendingProperty[]> {
    return this.http.get<PendingProperty[]>(`${this.base}/properties/pending-verification`);
  }

  updateVerification(id: string, body: UpdateVerificationRequest): Observable<Property> {
    return this.http.put<Property>(`${this.base}/properties/${id}/verification`, body).pipe(
      tap((updated) => {
        const list = this.propertiesSubject.value.map((p) =>
          p.id === id ? { ...p, ...updated } : p,
        );
        this.propertiesSubject.next(list);
      }),
    );
  }

  // Convenience helpers
  approve(id: string) {
    return this.updateVerification(id, { verificationStatus: verificationStatus.ManuallyVerified });
  }

  reject(id: string, reason: string) {
    return this.updateVerification(id, {
      verificationStatus: verificationStatus.Rejected,
      rejectionReason: reason,
    });
  }

  markInactive(id: string) {
    return this.updateVerification(id, { verificationStatus: verificationStatus.Rejected });
  }

  markActive(id: string) {
    return this.updateVerification(id, { verificationStatus: verificationStatus.ManuallyVerified });
  }
}
