import { Injectable, inject } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';

import { environment } from '../../../environments/environment';
import { NotificationService } from './notification.service';
import { TokenStorage } from './token-storage';

@Injectable()
export class NotificationRealtimeService {
  private readonly notifications = inject(NotificationService);
  private readonly tokenStorage = inject(TokenStorage);

  private connection: HubConnection | null = null;

  private queue: Promise<void> = Promise.resolve();

  private warned = false;

  start(): void {
    this.enqueue(async () => {
      await this.ensureStarted();
    });
  }

  stop(): void {
    this.enqueue(async () => {
      const connection = this.connection;
      this.connection = null;
      this.warned = false;

      await connection?.stop();
    });
  }

  private async ensureStarted(): Promise<void> {
    if (this.connection?.state === HubConnectionState.Connected) {
      return;
    }

    const token = this.tokenStorage.read()?.token;

    if (token === undefined) {
      return;
    }

    if (this.connection === null) {
      this.connection = this.build();
    }

    try {
      if (this.connection.state === HubConnectionState.Disconnected) {
        await this.connection.start();
      }
    } catch {
      this.warnOnce();
    }
  }

  private build(): HubConnection {
    const connection = new HubConnectionBuilder()
      .withUrl(environment.notificationHubUrl, {
        accessTokenFactory: () => this.tokenStorage.read()?.token ?? ''
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Critical)
      .build();

    connection.on('notificationReceived', () => this.refresh());

    connection.onreconnected(() => this.refresh());

    return connection;
  }

  private refresh(): void {
    this.notifications.loadUnreadCount().subscribe({
      error: () => this.warnOnce()
    });

    if (this.notifications.notifications().length > 0) {
      this.notifications.load().subscribe({ error: () => this.warnOnce() });
    }
  }

  private enqueue(work: () => Promise<void>): void {
    this.queue = this.queue
      .then(work)
      .catch(() => this.warnOnce());
  }

  private warnOnce(): void {
    if (this.warned) {
      return;
    }

    this.warned = true;
    console.warn('Live notifications are unavailable. The bell still loads them when opened.');
  }
}
