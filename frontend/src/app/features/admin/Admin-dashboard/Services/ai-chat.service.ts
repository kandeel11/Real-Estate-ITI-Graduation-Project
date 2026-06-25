import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, delay, map } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { API_ENDPOINTS } from '../../../../core/constants/api-endpoints';
import { ChatSession, SendMessageRequest, SendMessageResponse } from '../Models/chat.model';

@Injectable({ providedIn: 'root' })
export class AiChatService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/AI/chatbot`;

  // ── Admin Analytics Chat (real endpoint) ──────────────────────────────────
  sendAnalyticsMessage(prompt: string): Observable<string> {
    return this.http
      .post<{ answer: string }>(
        `${environment.apiUrl}${API_ENDPOINTS.AI.ADMIN_ANALYTICS_CHAT}`,
        { prompt }
      )
      .pipe(map((res) => res.answer));
  }

  // ── User Chatbot (kept for other uses) ────────────────────────────────────
  sendMessage(req: SendMessageRequest): Observable<SendMessageResponse> {
    return this.http.post<SendMessageResponse>(`${this.base}/chat`, req);
  }

  getSessions(): Observable<ChatSession[]> {
    return of(this.mockSessions()).pipe(delay(200));
  }

  getHistory(sessionId: string): Observable<SendMessageResponse[]> {
    return this.http.get<SendMessageResponse[]>(
      `${this.base}/sessions/${sessionId}/history`
    );
  }

  // ── Mock sessions (local-only history) ────────────────────────────────────
  private mockSessions(): ChatSession[] {
    return [
      {
        id: 's1',
        title: 'Revenue overview',
        lastMessage: 'Total collected is EGP 2.3M this month…',
        updatedAt: new Date(Date.now() - 3600e3).toISOString(),
        messageCount: 5,
      },
      {
        id: 's2',
        title: 'New listings this week',
        lastMessage: '47 new properties were added…',
        updatedAt: new Date(Date.now() - 86400e3).toISOString(),
        messageCount: 3,
      },
      {
        id: 's3',
        title: 'Subscription plans',
        lastMessage: 'Premium plan leads with 62% share…',
        updatedAt: new Date(Date.now() - 172800e3).toISOString(),
        messageCount: 6,
      },
    ];
  }
}
