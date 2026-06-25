import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { API_ENDPOINTS } from '../constants/api-endpoints';
import {
  WalletBalance,
  LedgerResponse,
  WithdrawalRequest,
  CreateWithdrawalRequest,
} from '../models/wallet.model';

@Injectable({ providedIn: 'root' })
export class WalletService {
  private http = inject(HttpClient);
  private base = environment.apiUrl;

  getBalance(): Observable<WalletBalance> {
    return this.http.get<WalletBalance>(`${this.base}${API_ENDPOINTS.WALLET.BALANCE}`);
  }

  getLedger(page = 1, pageSize = 20): Observable<LedgerResponse> {
    return this.http.get<LedgerResponse>(
      `${this.base}${API_ENDPOINTS.WALLET.LEDGER}?page=${page}&pageSize=${pageSize}`,
    );
  }

  requestWithdrawal(body: CreateWithdrawalRequest): Observable<{ withdrawalId: string }> {
    return this.http.post<{ withdrawalId: string }>(
      `${this.base}${API_ENDPOINTS.WALLET.WITHDRAW}`,
      body,
    );
  }

  getMyWithdrawals(): Observable<WithdrawalRequest[]> {
    return this.http.get<WithdrawalRequest[]>(
      `${this.base}${API_ENDPOINTS.WALLET.MY_WITHDRAWALS}`,
    );
  }

  // Admin
  getAllWithdrawals(): Observable<WithdrawalRequest[]> {
    return this.http.get<WithdrawalRequest[]>(
      `${this.base}${API_ENDPOINTS.WALLET.ALL_WITHDRAWALS}`,
    );
  }

  approveWithdrawal(id: string): Observable<void> {
    return this.http.put<void>(
      `${this.base}${API_ENDPOINTS.WALLET.APPROVE_WITHDRAWAL(id)}`,
      {},
    );
  }

  rejectWithdrawal(id: string, notes: string): Observable<void> {
    return this.http.put<void>(
      `${this.base}${API_ENDPOINTS.WALLET.REJECT_WITHDRAWAL(id)}`,
      { notes },
    );
  }
}
