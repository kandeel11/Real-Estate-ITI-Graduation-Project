import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../../../environments/environment';
import { UsersResponse } from '../Models/user.model';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class UserService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/users`;

  getUsers(params: {
    page: number;
    pageSize: number;
    search?: string;
    role?: string;
  }): Observable<UsersResponse> {
    let p = new HttpParams().set('page', params.page).set('pageSize', params.pageSize);
    if (params.search?.trim()) p = p.set('search', params.search.trim());
    if (params.role && params.role !== 'all') p = p.set('role', params.role);
    return this.http.get<UsersResponse>(this.base, { params: p });
  }

  banUser(id: string, reason: string): Observable<void> {
    return this.http.put<void>(`${this.base}/${id}/ban`, { reason });
  }

  unbanUser(id: string): Observable<void> {
    return this.http.put<void>(`${this.base}/${id}/unban`, {});
  }
}
