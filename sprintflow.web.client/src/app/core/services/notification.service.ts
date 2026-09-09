import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AppNotification, UnreadNotificationCount } from '../models/notification-models';

/**
 * Owns the signed-in user's notifications.
 *
 * A singleton with signals for the same reason AreaService is one: the badge in the header and the
 * panel it opens have to agree, and a new notification must reach both without a reload.
 *
 * The server decides everything. Which notifications these are is a filter applied there against
 * the caller's token — nothing here compares a user id, and there is no request this client could
 * make that would return somebody else's rows.
 */
@Injectable()
export class NotificationService {
  private readonly http = inject(HttpClient);

  private readonly notificationState = signal<readonly AppNotification[]>([]);
  private readonly unreadCountState = signal(0);

  readonly notifications = this.notificationState.asReadonly();

  /**
   * What the badge shows. Kept separately from the list, and refreshed on its own: the badge is
   * live from the moment the shell loads, while the list is only worth reading when the panel is
   * actually opened.
   */
  readonly unreadCount = this.unreadCountState.asReadonly();

  /** The panel's contents. Replaced wholesale by the server's answer. */
  load(): Observable<readonly AppNotification[]> {
    return this.http
      .get<readonly AppNotification[]>(`${environment.apiBaseUrl}/notifications`)
      .pipe(tap(notifications => this.notificationState.set(notifications)));
  }

  /** One cheap count, which is all the header needs. */
  loadUnreadCount(): Observable<UnreadNotificationCount> {
    return this.http
      .get<UnreadNotificationCount>(`${environment.apiBaseUrl}/notifications/unread-count`)
      .pipe(tap(result => this.unreadCountState.set(result.unreadCount)));
  }

  /**
   * Marks one read. The local row and the count are updated from what was just asked for rather
   * than re-fetched — the server has accepted it by the time this runs, and a second round trip
   * would only redraw the same panel.
   */
  markRead(notificationId: number): Observable<void> {
    return this.http
      .put<void>(`${environment.apiBaseUrl}/notifications/${notificationId}/read`, null)
      .pipe(tap(() => this.applyRead(notificationId)));
  }

  markAllRead(): Observable<void> {
    return this.http
      .put<void>(`${environment.apiBaseUrl}/notifications/read-all`, null)
      .pipe(tap(() => {
        this.notificationState.update(notifications =>
          notifications.map(notification => ({ ...notification, isRead: true })));

        this.unreadCountState.set(0);
      }));
  }

  /** Dropped on sign-out, so the next person to sign in never sees the last one's notifications. */
  clear(): void {
    this.notificationState.set([]);
    this.unreadCountState.set(0);
  }

  private applyRead(notificationId: number): void {
    const target = this.notificationState()
      .find(notification => notification.notificationId === notificationId);

    // Already read, or not in the loaded page: either way the count must not be decremented.
    if (target === undefined || target.isRead) {
      return;
    }

    this.notificationState.update(notifications => notifications.map(notification =>
      notification.notificationId === notificationId
        ? { ...notification, isRead: true }
        : notification));

    this.unreadCountState.update(count => Math.max(0, count - 1));
  }
}
