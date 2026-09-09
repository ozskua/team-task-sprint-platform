import { Injectable, inject } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';

import { environment } from '../../../environments/environment';
import { NotificationService } from './notification.service';
import { TokenStorage } from './token-storage';

/**
 * Keeps the notification badge in step with what other people are doing.
 *
 * The same shape as BoardRealtimeService, and deliberately no more than that — one connection, one
 * event, and a re-read through the service that already owns the data. The difference is who the
 * event is addressed to: the board hub sends to a group, this one is sent to a person, so there is
 * no group to join and nothing for this class to keep track of but the connection itself.
 *
 * Realtime is an improvement, never a requirement. Every failure is swallowed after one warning: if
 * the hub cannot be reached the bell still works, it simply refreshes when it is opened rather than
 * on its own.
 */
@Injectable()
export class NotificationRealtimeService {
  private readonly notifications = inject(NotificationService);
  private readonly tokenStorage = inject(TokenStorage);

  private connection: HubConnection | null = null;

  /** Serialises start and stop, so a fast sign-out cannot interleave with a connection opening. */
  private queue: Promise<void> = Promise.resolve();

  /** Said once per connection rather than on every retry, which is what would fill the console. */
  private warned = false;

  /** Called by the bell when the shell appears. Repeated calls do nothing. */
  start(): void {
    this.enqueue(async () => {
      await this.ensureStarted();
    });
  }

  /**
   * Closes the connection. Called when the shell goes away, which is what signing out does — and
   * the connection has to go with it, or the next person to sign in would keep receiving the
   * previous user's notifications on a socket authenticated as them.
   */
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

    // Signed out: there is nothing to authenticate with, and the hub would refuse the connection.
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
        // Read fresh on every attempt, so a reconnect after signing in again uses the new token —
        // which is also what makes the server address the right person.
        accessTokenFactory: () => this.tokenStorage.read()?.token ?? ''
      })
      .withAutomaticReconnect()
      // Silenced because the client logs every failed retry at Error level, and a backend that is
      // simply not running would fill the console with them. This class says it once instead.
      .configureLogging(LogLevel.Critical)
      .build();

    // The event carries nothing. What arrived is read over REST, which stays the source of truth.
    connection.on('notificationReceived', () => this.refresh());

    // Whatever was sent while the connection was down was never delivered, so the count is re-read
    // rather than waited for.
    connection.onreconnected(() => this.refresh());

    return connection;
  }

  /**
   * Re-reads the badge, and the list too when one has already been opened — so a panel left open
   * gains the new row instead of showing a count that disagrees with what is under it.
   */
  private refresh(): void {
    this.notifications.loadUnreadCount().subscribe({
      // A failed refresh leaves the last known count, which is better than clearing the badge.
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
