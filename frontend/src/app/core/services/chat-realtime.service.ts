import { Injectable, NgZone, inject } from '@angular/core';
import { Observable, Subject, filter, map } from 'rxjs';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

/** Shape broadcast by the backend hub for each live chat message. */
export interface RealtimeChatMessage {
  id: string;
  senderId: string;
  senderName: string;
  senderRole: 'broker' | 'tenant' | 'owner';
  text: string;
  sentAt: string;
}

interface ReceivePayload {
  inquiryId: string;
  message: RealtimeChatMessage;
}

/**
 * Single shared SignalR connection to the backend chat hub (`/hubs/chat`).
 *
 * Chat threads are keyed by inquiry id: callers `join(inquiryId)` to start
 * receiving live messages for that thread and subscribe via `on(inquiryId)`.
 * The connection is built lazily on first use and reused everywhere.
 */
@Injectable({ providedIn: 'root' })
export class ChatRealtimeService {
  private readonly auth = inject(AuthService);
  private readonly zone = inject(NgZone);

  private connection?: HubConnection;
  private starting?: Promise<void>;
  private readonly incoming$ = new Subject<ReceivePayload>();

  /** Inquiry ids we've asked to join, so we can re-join after a reconnect. */
  private readonly joined = new Set<string>();

  /** Hub lives at the host root (`/hubs/chat`), not under the `/api` prefix. */
  private get hubUrl(): string {
    return environment.apiUrl.replace(/\/api\/?$/, '') + '/hubs/chat';
  }

  private ensureConnection(): HubConnection {
    if (this.connection) return this.connection;

    this.connection = new HubConnectionBuilder()
      .withUrl(this.hubUrl, {
        accessTokenFactory: () => this.auth.getToken() ?? '',
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    // Hub callbacks fire outside Angular's zone — re-enter it so component
    // signal updates trigger change detection (render + auto-scroll).
    this.connection.on('ReceiveMessage', (payload: ReceivePayload) => {
      if (payload?.inquiryId && payload.message) {
        this.zone.run(() => this.incoming$.next(payload));
      }
    });

    // Re-join every active thread after the transport reconnects.
    this.connection.onreconnected(() => {
      this.joined.forEach((id) => this.connection?.invoke('JoinInquiry', id).catch(() => {}));
    });

    return this.connection;
  }

  private async start(): Promise<void> {
    const conn = this.ensureConnection();
    if (conn.state === HubConnectionState.Connected) return;
    if (!this.starting) {
      this.starting = conn.start().finally(() => (this.starting = undefined));
    }
    return this.starting;
  }

  /** Join an inquiry thread to start receiving its live messages. */
  async join(inquiryId: string): Promise<void> {
    this.joined.add(inquiryId);
    try {
      await this.start();
      await this.connection!.invoke('JoinInquiry', inquiryId);
    } catch {
      // Real-time is best-effort; the HTTP fetch still backs the UI.
    }
  }

  /** Join several inquiry threads at once (e.g. every thread in a list). */
  async joinMany(inquiryIds: string[]): Promise<void> {
    inquiryIds.forEach((id) => this.joined.add(id));
    try {
      await this.start();
      await Promise.all(
        inquiryIds.map((id) => this.connection!.invoke('JoinInquiry', id).catch(() => {})),
      );
    } catch {
      /* best-effort */
    }
  }

  /** Leave an inquiry thread when its view closes. */
  async leave(inquiryId: string): Promise<void> {
    this.joined.delete(inquiryId);
    if (this.connection?.state === HubConnectionState.Connected) {
      try {
        await this.connection.invoke('LeaveInquiry', inquiryId);
      } catch {
        /* ignore */
      }
    }
  }

  /** Stream of live messages for a single inquiry thread. */
  on(inquiryId: string): Observable<RealtimeChatMessage> {
    return this.incoming$.pipe(
      filter((p) => p.inquiryId === inquiryId),
      map((p) => p.message),
    );
  }

  /** Stream of live messages across every joined inquiry thread. */
  onAny(): Observable<ReceivePayload> {
    return this.incoming$.asObservable();
  }

  /** Leave every joined thread (e.g. when a list view is destroyed). */
  async leaveAll(): Promise<void> {
    const ids = [...this.joined];
    this.joined.clear();
    if (this.connection?.state === HubConnectionState.Connected) {
      await Promise.all(
        ids.map((id) => this.connection!.invoke('LeaveInquiry', id).catch(() => {})),
      );
    }
  }
}
