
export type NotificationType =
  | 'client'
  | 'followup'
  | 'note';

export interface AppNotification {
  id: string;
  type: NotificationType;
  title: string;
  message: string;
  entityId: number;
  createdAt: Date;
  isRead: boolean;
}
