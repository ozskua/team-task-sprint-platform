export type NotificationType = 1 | 2 | 3 | 4;

export interface AppNotification {
  readonly notificationId: number;
  readonly type: NotificationType;

  readonly actorUserId: number;
  readonly actorFullName: string;

  readonly taskId: number;
  readonly taskTitle: string;

  readonly boardId: number;

  readonly isRead: boolean;

  readonly createdDate: string;
}

export interface UnreadNotificationCount {
  readonly unreadCount: number;
}
