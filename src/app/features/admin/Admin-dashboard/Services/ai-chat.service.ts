import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, delay } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { ChatSession, SendMessageRequest, SendMessageResponse } from '../Models/chat.model';

@Injectable({ providedIn: 'root' })
export class AiChatService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/AI/chatbot`;

  // Toggle this to false when backend is ready
  private useMock = true;

  sendMessage(req: SendMessageRequest): Observable<SendMessageResponse> {
    if (this.useMock) {
      return of(this.mockReply(req)).pipe(delay(900));
    }
    return this.http.post<SendMessageResponse>(`${this.base}/chat`, req);
  }

  getSessions(): Observable<ChatSession[]> {
    if (this.useMock) {
      return of(this.mockSessions()).pipe(delay(300));
    }
    return this.http.get<ChatSession[]>(`${this.base}/sessions`);
  }

  getHistory(sessionId: string): Observable<SendMessageResponse[]> {
    if (this.useMock) return of([]).pipe(delay(200));
    return this.http.get<SendMessageResponse[]>(`${this.base}/sessions/${sessionId}/history`);
  }

  // ===== MOCK =====
  private mockReply(req: SendMessageRequest): SendMessageResponse {
    const q = req.message.toLowerCase();
    let reply = '';
    if (q.includes('market') || q.includes('سوق') || q.includes('price') || q.includes('سعر')) {
      reply = `Based on the latest market data:\n\n• **Average rent** is up 12.4% YoY, now ~EGP 8,450/mo\n• **Zamalek** leads growth at +22.1%\n• **New Cairo** has the most listings (521)\n\nAvg. days on market dropped to 18 days — demand is strong. Want me to break this down by district?`;
    } else if (q.includes('user') || q.includes('يوزر') || q.includes('مستخدم')) {
      reply = `You currently have **5 registered users**:\n\n• 1 Admin\n• 1 Owner\n• 1 Tenant\n• 1 Broker\n• 1 Maintenance Company\n\nNo banned users right now. Want a breakdown of activity or last-login stats?`;
    } else if (q.includes('property') || q.includes('عقار') || q.includes('listing')) {
      reply = `Your portfolio overview:\n\n• Active listings are performing well\n• A few properties are still **Pending Review**\n• Apartments make up 43.6% of listings\n\nWould you like me to highlight listings that need verification?`;
    } else {
      reply = `Got it! I'm your real estate AI assistant. I can help you analyze:\n\n• 📊 Market trends & pricing\n• 👥 User activity\n• 🏠 Property listings & verification\n• 💰 Revenue & maintenance tickets\n\nWhat would you like to explore?`;
    }
    return {
      sessionId: req.sessionId ?? crypto.randomUUID(),
      replyText: reply,
    };
  }

  private mockSessions(): ChatSession[] {
    return [
      {
        id: 's1',
        title: 'Market trends Q2',
        lastMessage: 'Zamalek leads growth at +22%…',
        updatedAt: new Date(Date.now() - 3600e3).toISOString(),
        messageCount: 8,
      },
      {
        id: 's2',
        title: 'User activity report',
        lastMessage: 'You have 5 registered users…',
        updatedAt: new Date(Date.now() - 86400e3).toISOString(),
        messageCount: 4,
      },
      {
        id: 's3',
        title: 'Pending verifications',
        lastMessage: '3 properties need review…',
        updatedAt: new Date(Date.now() - 172800e3).toISOString(),
        messageCount: 6,
      },
    ];
  }
}
