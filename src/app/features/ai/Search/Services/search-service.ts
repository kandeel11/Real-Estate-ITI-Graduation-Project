import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError, timeout } from 'rxjs/operators';
import { environment } from '../../../../../environments/environment';
import { SmartSearchRequest, SmartSearchResponse } from '../models';

@Injectable({ providedIn: 'root' })
export class SearchService {
  private http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/search`;

  /**
   * AI-powered smart search.
   * Accepts natural language (Arabic or English) and returns the top properties.
   */
  smartSearch(payload: SmartSearchRequest): Observable<SmartSearchResponse> {
    const body = {
      query: payload.query,
      page: payload.page ?? 1,
      pageSize: payload.pageSize ?? 5,
    };

    return this.http
      .post<SmartSearchResponse>(`${this.baseUrl}/smart`, body)
      .pipe(timeout(30_000), catchError(this.handleError));
  }

  private handleError(err: HttpErrorResponse | any) {
    console.error('[SearchService] error:', err);
    const message = err?.error?.message || err?.message || 'Something went wrong while searching.';
    return throwError(() => new Error(message));
  }
}
