/**
 * Company AI Service
 * Chat against the backend AI chatbot (POST /api/AI/chat).
 * Contract: { userId, sessionId?, message } → { sessionId, replyText }.
 */

import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface ChatReply {
  sessionId: string;
  replyText: string;
}

@Injectable({ providedIn: 'root' })
export class CompanyAiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  chat(userId: string, sessionId: string | null, message: string): Observable<ChatReply> {
    return this.http.post<ChatReply>(`${this.baseUrl}/AI/chat`, {
      userId,
      sessionId,
      message,
    });
  }
}
