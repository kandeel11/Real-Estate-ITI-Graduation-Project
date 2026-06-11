import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, catchError } from 'rxjs';
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
  conversationId?: string;
  leaseId?: string;
}

@Injectable({ providedIn: 'root' })
export class AiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  chat(messages: AiChatMessage[]): Observable<AiChatResponse> {
    return this.http.post<AiChatResponse>(`${this.base}/AI/chat`, { messages });
  }

  chatRag(data: ChatRagRequest): Observable<AiChatResponse> {
    return this.http.post<AiChatResponse>(`${this.base}/ChatRag/message`, data);
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
