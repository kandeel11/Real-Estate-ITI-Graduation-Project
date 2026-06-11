import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { ChatRequest, ChatResponse, HistoryMessage, SessionSummary } from '../models/index';

@Injectable({ providedIn: 'root' })
export class ChatbotService {
  private readonly baseUrl = '/api/AI/chatbot';

  // Shared state so the widget and the dashboard stay in sync
  private readonly _isOpen = new BehaviorSubject<boolean>(false);
  readonly isOpen$ = this._isOpen.asObservable();

  private readonly _activeSessionId = new BehaviorSubject<string | null>(null);
  readonly activeSessionId$ = this._activeSessionId.asObservable();

  constructor(private http: HttpClient) {}

  // ---- UI helpers ----
  open(sessionId?: string) {
    if (sessionId) this._activeSessionId.next(sessionId);
    this._isOpen.next(true);
  }
  close() {
    this._isOpen.next(false);
  }
  toggle() {
    this._isOpen.next(!this._isOpen.value);
  }
  setActiveSession(id: string | null) {
    this._activeSessionId.next(id);
  }

  // ---- API ----
  sendMessage(payload: ChatRequest): Observable<ChatResponse> {
    return this.http.post<ChatResponse>(`${this.baseUrl}/chat`, payload);
  }

  getSessions(): Observable<SessionSummary[]> {
    return this.http.get<SessionSummary[]>(`${this.baseUrl}/sessions`);
  }

  getHistory(sessionId: string): Observable<HistoryMessage[]> {
    return this.http.get<HistoryMessage[]>(`${this.baseUrl}/sessions/${sessionId}/history`);
  }

  // ---- utils ----
  generateSessionId(): string {
    if (typeof crypto !== 'undefined' && 'randomUUID' in crypto) {
      return (crypto as any).randomUUID();
    }
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
      const r = (Math.random() * 16) | 0;
      const v = c === 'x' ? r : (r & 0x3) | 0x8;
      return v.toString(16);
    });
  }
}
