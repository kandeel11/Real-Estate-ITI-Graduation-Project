import { Injectable, NgZone, inject } from '@angular/core';
import { Observable, Subject } from 'rxjs';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

/** Subset of fields the notification hub pushes — always an unread, fresh notification. */
export interface PushNotification {
  id: string;
  type: string;
  title: string;
  body: string;
  relatedEntityType: string | null;
  relatedEntityId: string | null;
  createdAt: string;
}

/**
 * Real-time notification delivery via SignalR `/hubs/notifications`.
 * The server places each user into their personal group on connect —
 * no explicit join is needed. HTTP polling in NotificationService acts
 * as a fallback when the hub is unavailable.
 */
@Injectable({ providedIn: 'root' })
export class NotificationRealtimeService {
  private readonly auth = inject(AuthService);
  private readonly zone = inject(NgZone);

  private connection?: HubConnection;
  private starting?: Promise<void>;
  private readonly incoming$ = new Subject<PushNotification>();

  private get hubUrl(): string {
    return environment.apiUrl.replace(/\/api\/?$/, '') + '/hubs/notifications';
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

    this.connection.on('ReceiveNotification', (n: PushNotification) => {
      if (n?.id) {
        this.zone.run(() => this.incoming$.next(n));
      }
    });

    return this.connection;
  }

  /** Connect to the hub (best-effort) and return a stream of pushed notifications. */
  start(): Observable<PushNotification> {
    const conn = this.ensureConnection();
    if (conn.state !== HubConnectionState.Connected && !this.starting) {
      this.starting = conn.start()
        .catch(() => { /* hub unavailable — HTTP polling is the fallback */ })
        .finally(() => (this.starting = undefined));
    }
    return this.incoming$.asObservable();
  }

  stop(): void {
    this.incoming$.complete();
    this.connection?.stop().catch(() => {});
    this.connection = undefined;
  }
}
