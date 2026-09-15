import { ChangeDetectionStrategy, Component, DestroyRef, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';

import { AppNotification, NotificationType } from '../../core/models/notification-models';
import { NotificationRealtimeService } from '../../core/services/notification-realtime.service';
import { NotificationService } from '../../core/services/notification.service';

const NOTIFICATION_TEXTS: Readonly<Record<NotificationType, string>> = {
  1: 'size bir görev atadı',
  2: 'bir göreve yorum yaptı',
  3: 'bir görevi tamamladı',
  4: 'bir görevi iptal etti'
};

@Component({
  selector: 'app-notification-bell',
  templateUrl: './notification-bell.html',
  standalone: false,
  styleUrl: './notification-bell.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NotificationBell {
  readonly boardRoutePrefix = input.required<string>();

  private readonly notificationService = inject(NotificationService);
  private readonly realtime = inject(NotificationRealtimeService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly notifications = this.notificationService.notifications;
  protected readonly unreadCount = this.notificationService.unreadCount;

  protected readonly loading = signal(false);
  protected readonly loadFailed = signal(false);

  protected readonly loadedOnce = signal(false);

  constructor() {
    this.notificationService.loadUnreadCount()
      .pipe(takeUntilDestroyed())
      .subscribe({ error: () => undefined });

    this.realtime.start();

    this.destroyRef.onDestroy(() => {
      this.realtime.stop();
      this.notificationService.clear();
    });
  }

  protected onOpened(): void {
    this.loading.set(true);
    this.loadFailed.set(false);

    this.notificationService.load()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.loading.set(false);
          this.loadedOnce.set(true);
        },
        error: () => {
          this.loading.set(false);
          this.loadFailed.set(true);
        }
      });
  }

  protected text(notification: AppNotification): string {
    return `${notification.actorFullName} ${NOTIFICATION_TEXTS[notification.type]}`;
  }

  protected open(notification: AppNotification): void {
    if (!notification.isRead) {
      this.notificationService.markRead(notification.notificationId)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({ error: () => undefined });
    }

    void this.router.navigate([this.boardRoutePrefix(), notification.boardId]);
  }

  protected markAllRead(): void {
    if (this.unreadCount() === 0) {
      return;
    }

    this.notificationService.markAllRead()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ error: () => this.loadFailed.set(true) });
  }
}
