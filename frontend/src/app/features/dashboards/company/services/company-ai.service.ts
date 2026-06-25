/**
 * Company AI Service
 * Chat against the backend AI chatbot (POST /api/AI/chatbot/chat).
 * Contract: { sessionId?, message } → { sessionId, replyText } — the user
 * is taken from the JWT on the backend.
 */

import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { API_ENDPOINTS } from '../../../../core/constants/api-endpoints';

export interface ChatReply {
  sessionId: string;
  replyText: string;
}

@Injectable({ providedIn: 'root' })
export class CompanyAiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  chat(sessionId: string | null, message: string): Observable<ChatReply> {
    const body = sessionId ? { sessionId, message } : { message };
    return this.http.post<ChatReply>(
      `${this.baseUrl}${API_ENDPOINTS.AI.CHATBOT_CHAT}`,
      body
    );
  }
}
