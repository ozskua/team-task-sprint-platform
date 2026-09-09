/**
 * What happened, as the server's `NotificationType` numbers it. A union of literals rather than a
 * TypeScript enum, exactly like `TaskPriority` — the domain keeps the numbers and nothing here
 * re-declares them.
 *
 * 1 TaskAssigned · 2 TaskCommented · 3 TaskCompleted · 4 TaskCancelled
 */
export type NotificationType = 1 | 2 | 3 | 4;

/**
 * One notification as the server returns it.
 *
 * There is no message text: the sentence is composed on screen from the type and the two names
 * below, so a task renamed afterwards is never quoted by its old title.
 */
export interface AppNotification {
  readonly notificationId: number;
  readonly type: NotificationType;

  /** Who did the thing. Shown, never compared — this client makes no authorization decision. */
  readonly actorUserId: number;
  readonly actorFullName: string;

  readonly taskId: number;
  readonly taskTitle: string;

  /** Where clicking the notification leads. */
  readonly boardId: number;

  readonly isRead: boolean;

  /** ISO date-time without an offset, exactly as the server stored it. */
  readonly createdDate: string;
}

/** The badge's endpoint. An object rather than a bare number, as the server sends it. */
export interface UnreadNotificationCount {
  readonly unreadCount: number;
}
