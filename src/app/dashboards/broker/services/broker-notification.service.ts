import { Injectable, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class BrokerNotificationService {
  private hub: signalR.HubConnection | null = null;

  inquiryCount = signal(3); // start with mock count
  connected = signal(false);

  connect() {
    if (this.hub) return;

    this.hub = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiUrl.replace('/api', '')}/hubs/broker`)
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.hub.on('NewInquiry', () => {
      this.inquiryCount.update((n) => n + 1);
    });

    this.hub.on('InquiryRead', () => {
      this.inquiryCount.update((n) => Math.max(0, n - 1));
    });

    this.hub
      .start()
      .then(() => this.connected.set(true))
      .catch(() => {
        // Backend not available in dev — mock mode continues
        console.info('[BrokerHub] Running in mock mode (no SignalR backend)');
      });
  }

  disconnect() {
    this.hub?.stop();
    this.hub = null;
    this.connected.set(false);
  }
}
