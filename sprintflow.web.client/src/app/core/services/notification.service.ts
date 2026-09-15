import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AppNotification, UnreadNotificationCount } from '../models/notification-models';

@Injectable()
export class NotificationService {
  private readonly http = inject(HttpClient);

  private readonly notificationState = signal<readonly AppNotification[]>([]);
  private readonly unreadCountState = signal(0);

  readonly notifications = this.notificationState.asReadonly();

  readonly unreadCount = this.unreadCountState.asReadonly();

  load(): Observable<readonly AppNotification[]> {
    return this.http
      .get<readonly AppNotification[]>(`${environment.apiBaseUrl}/notifications`)
      .pipe(tap(notifications => this.notificationState.set(notifications)));
  }

  loadUnreadCount(): Observable<UnreadNotificationCount> {
    return this.http
      .get<UnreadNotificationCount>(`${environment.apiBaseUrl}/notifications/unread-count`)
      .pipe(tap(result => this.unreadCountState.set(result.unreadCount)));
  }

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

  clear(): void {
    this.notificationState.set([]);
    this.unreadCountState.set(0);
  }

  private applyRead(notificationId: number): void {
    const target = this.notificationState()
      .find(notification => notification.notificationId === notificationId);

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
