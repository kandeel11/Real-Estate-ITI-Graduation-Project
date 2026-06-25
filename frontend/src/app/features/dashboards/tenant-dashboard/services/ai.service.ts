import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, catchError, map } from 'rxjs';
import { environment } from '../../../../../environments/environment';

export interface AiChatMessage {
  role: 'user' | 'assistant';
  content: string;
}

export interface AiChatResponse {
  message: string;
}

export interface SmartSearchResult {
  id: string;
  title: string;
  address: string;
  price: number;
  bedrooms: number;
  bathrooms: number;
  area: number;
  mainImage: string;
  type: string;
}

export interface SavedSearch {
  id: string;
  query: string;
  createdAt: string;
}

export interface ChatRagRequest {
  message: string;
  sessionId?: string;
  leaseId?: string;
}

@Injectable({ providedIn: 'root' })
export class AiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  // Backend contract: POST /AI/chatbot/chat { sessionId?, message } →
  // { sessionId, replyText }; the user comes from the JWT. The session id is
  // tracked here so the page can keep passing the whole message list.
  private chatSessionId: string | null = null;

  // ChatRag requires SessionId on every call — generate one per conversation.
  private chatRagSessionId: string = crypto.randomUUID();

  chat(messages: AiChatMessage[]): Observable<AiChatResponse> {
    const lastUserMessage =
      [...messages].reverse().find((m) => m.role === 'user')?.content ?? '';
    const body = this.chatSessionId
      ? { sessionId: this.chatSessionId, message: lastUserMessage }
      : { message: lastUserMessage };

    return this.http
      .post<{ sessionId: string; replyText: string }>(`${this.base}/AI/chatbot/chat`, body)
      .pipe(
        map((res) => {
          this.chatSessionId = res.sessionId;
          return { message: res.replyText };
        })
      );
  }

  chatRag(data: ChatRagRequest): Observable<AiChatResponse> {
    return this.http
      .post<Record<string, string>>(`${this.base}/ChatRag/message`, {
        ...data,
        sessionId: this.chatRagSessionId,
      })
      .pipe(
        map((res) => ({
          // Backend may return message / replyText / response / answer
          message: res['message'] ?? res['replyText'] ?? res['response'] ?? res['answer'] ?? '',
        }))
      );
  }

  /** Call before opening a new chat conversation to get a fresh session. */
  resetChatRagSession(): void {
    this.chatRagSessionId = crypto.randomUUID();
  }

  smartSearch(query: string, filters?: Record<string, unknown>): Observable<SmartSearchResult[]> {
    return this.http.post<SmartSearchResult[]>(`${this.base}/search/smart`, { query, ...filters })
      .pipe(catchError(() => of([])));
  }

  saveSearch(query: string): Observable<SavedSearch> {
    return this.http.post<SavedSearch>(`${this.base}/search/save`, { query });
  }

  getSavedSearches(): Observable<SavedSearch[]> {
    return this.http.get<SavedSearch[]>(`${this.base}/search/saved`).pipe(catchError(() => of([])));
  }

  deleteSavedSearch(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/search/saved/${id}`);
  }
}
