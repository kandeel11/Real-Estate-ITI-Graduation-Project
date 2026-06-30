import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { environment } from '../../../../../environments/environment';
import { ChatRequest, ChatResponse, ChatRole, HistoryMessage, SessionSummary } from '../models';

@Injectable({ providedIn: 'root' })
export class ChatbotService {
  private readonly baseUrl = `${environment.apiUrl}/AI/chatbot`;

  private readonly _isOpen = new BehaviorSubject<boolean>(false);
  readonly isOpen$ = this._isOpen.asObservable();

  private readonly _activeSessionId = new BehaviorSubject<string | null>(null);
  readonly activeSessionId$ = this._activeSessionId.asObservable();

  constructor(private http: HttpClient) {}

  // ---- UI ----
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

  /**
   * POST /api/AI/chatbot/chat
   * - First call: send WITHOUT sessionId → backend creates one and returns it
   * - Following calls: send WITH sessionId → backend continues the same session
   */
  sendMessage(payload: ChatRequest): Observable<ChatResponse> {
    // shape body: only include sessionId if it exists
    const body: ChatRequest = payload.sessionId
      ? { sessionId: payload.sessionId, message: payload.message }
      : { message: payload.message };

    return this.http
      .post<ChatResponse>(`${this.baseUrl}/chat`, body)
      .pipe(catchError(this.handleError));
  }

  getSessions(): Observable<SessionSummary[]> {
    return this.http
      .get<any[]>(`${this.baseUrl}/sessions`)
      .pipe(
        map(list => (list ?? []).map(s => ({
          sessionId: s.id,
          title: s.title,
          createdAt: s.createdAt,
          lastMessageAt: s.createdAt,
        } as SessionSummary))),
        catchError(this.handleError),
      );
  }

  getHistory(sessionId: string): Observable<HistoryMessage[]> {
    return this.http
      .get<any[]>(`${this.baseUrl}/sessions/${sessionId}/history`)
      .pipe(
        map(list => (list ?? []).map(m => ({
          role: m.role as ChatRole,
          message: m.content ?? m.message ?? '',
          createdAt: m.createdAt,
        } as HistoryMessage))),
        catchError(this.handleError),
      );
  }

  private handleError(error: any) {
    console.error('[Chatbot] API error:', error);
    const message = error.error?.message || error.message || 'Unexpected error';
    return throwError(() => new Error(message));
  }
}
