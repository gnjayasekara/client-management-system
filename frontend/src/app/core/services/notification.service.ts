
import {
  DestroyRef,
  Injectable,
  computed,
  inject,
  signal
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { RealtimeService } from './realtime.service';
import {
  AppNotification,
  NotificationType
} from '../models/notification.model';
import { RealtimeEvent } from '../models/real-time-event.model';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private readonly realtimeService = inject(RealtimeService);
  private readonly destroyRef = inject(DestroyRef);

  private readonly notificationState =
    signal<AppNotification[]>([]);

  readonly notifications = this.notificationState.asReadonly();

  readonly unreadCount = computed(() =>
    this.notificationState().filter(n => !n.isRead).length
  );

  constructor() {
    this.realtimeService.clientChanged$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(event => {
        this.addNotification('client', event);
      });

    this.realtimeService.followUpChanged$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(event => {
        this.addNotification('followup', event);
      });

    this.realtimeService.noteChanged$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(event => {
        this.addNotification('note', event);
      });
  }

  private addNotification(
    type: NotificationType,
    event: RealtimeEvent
  ): void {
    const names: Record<NotificationType, string> = {
      client: 'Client',
      followup: 'Follow-up',
      note: 'Client note'
    };

    const actions: Record<string, string> = {
      Created: 'created',
      Updated: 'updated',
      Deleted: 'deleted',
      Completed: 'completed'
    };

    const name = names[type];
    const action =
      actions[event.action] ?? event.action.toLowerCase();

    const notification: AppNotification = {
      id: crypto.randomUUID(),
      type,
      title: `${name} ${action}`,
      message: `${name} #${event.entityId} was ${action}.`,
      entityId: event.entityId,
      createdAt: new Date(),
      isRead: false
    };

    this.notificationState.update(current =>
      [notification, ...current].slice(0, 50)
    );
  }

  markAsRead(id: string): void {
    this.notificationState.update(items =>
      items.map(item =>
        item.id === id
          ? { ...item, isRead: true }
          : item
      )
    );
  }

  markAllAsRead(): void {
    this.notificationState.update(items =>
      items.map(item => ({
        ...item,
        isRead: true
      }))
    );
  }

  clear(): void {
    this.notificationState.set([]);
  }
}
