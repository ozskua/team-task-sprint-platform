import { ChangeDetectionStrategy, Component, DestroyRef, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';

import { AppNotification, NotificationType } from '../../core/models/notification-models';
import { NotificationRealtimeService } from '../../core/services/notification-realtime.service';
import { NotificationService } from '../../core/services/notification.service';

/**
 * The Turkish wording for each notification type.
 *
 * Here rather than on the server for the same reason `TASK_PRIORITY_LABELS` is on the client: the
 * domain keeps the numbers and only the screen speaks Turkish. It also means a task renamed after
 * the fact is never quoted by its old title, because no sentence was ever stored.
 */
const NOTIFICATION_TEXTS: Readonly<Record<NotificationType, string>> = {
  1: 'size bir görev atadı',
  2: 'bir göreve yorum yaptı',
  3: 'bir görevi tamamladı',
  4: 'bir görevi iptal etti'
};

/**
 * The bell in the header, shared by both shells and sitting beside the account menu.
 *
 * It owns no data: the count and the list live in NotificationService so the badge and the panel
 * cannot disagree, and every authorization decision belongs to the server — this never compares a
 * user id to decide what to show.
 */
@Component({
  selector: 'app-notification-bell',
  templateUrl: './notification-bell.html',
  standalone: false,
  styleUrl: './notification-bell.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NotificationBell {
  /**
   * Where a notification leads, supplied by whichever shell is rendering this. Both shells reach
   * the same board at their own URL, so a fixed path here would be wrong in one of them — the same
   * reason AreaNav is handed its prefix instead of choosing one.
   */
  readonly boardRoutePrefix = input.required<string>();

  private readonly notificationService = inject(NotificationService);
  private readonly realtime = inject(NotificationRealtimeService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly notifications = this.notificationService.notifications;
  protected readonly unreadCount = this.notificationService.unreadCount;

  protected readonly loading = signal(false);
  protected readonly loadFailed = signal(false);

  /** Until the panel has been opened once, "no notifications" would be a guess rather than a fact. */
  protected readonly loadedOnce = signal(false);

  constructor() {
    // The badge is live from the moment the shell appears, without opening anything.
    this.notificationService.loadUnreadCount()
      .pipe(takeUntilDestroyed())
      .subscribe({ error: () => undefined });

    this.realtime.start();

    // The shell goes away when the user signs out, and the connection and the data go with it —
    // otherwise the next person to sign in would briefly see the previous one's notifications.
    this.destroyRef.onDestroy(() => {
      this.realtime.stop();
      this.notificationService.clear();
    });
  }

  /** Read on every open: what is on screen should be what the server has, not what it had. */
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

  /**
   * Marks it read and goes to its board. The navigation does not wait for the server: the read
   * state is a convenience, and holding the page still while it is written would make every
   * notification feel slow. A failed write simply leaves it unread.
   */
  protected open(notification: AppNotification): void {
    if (!notification.isRead) {
      this.notificationService.markRead(notification.notificationId)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({ error: () => undefined });
    }

    void this.router.navigate([this.boardRoutePrefix(), notification.boardId]);
  }

  protected markAllRead(): void {
    // Nothing to clear, so nothing is sent.
    if (this.unreadCount() === 0) {
      return;
    }

    this.notificationService.markAllRead()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ error: () => this.loadFailed.set(true) });
  }
}
