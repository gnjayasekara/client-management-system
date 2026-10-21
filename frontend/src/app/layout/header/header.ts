
import { Component, inject, input, output, signal } from '@angular/core';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import { Router } from '@angular/router';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-header',
  templateUrl: './header.html',
  styleUrl: './header.css',
  imports: [
    DatePipe
  ]
})
export class Header {
  private readonly authService = inject(AuthService);

  readonly title = input<string>('Dashboard');
  readonly menuClicked = output<void>();

  readonly username = this.authService.username;
  readonly roles = this.authService.roles;

  private readonly router = inject(Router); 

  
  readonly notificationService = inject(NotificationService);

  readonly notificationsOpen = signal(false);

  toggleNotifications(): void {
    this.notificationsOpen.update(value => !value);
  }

  closeNotifications(): void {
    this.notificationsOpen.set(false);
  }

  markAllNotificationsRead(): void {
    this.notificationService.markAllAsRead();
  }

  openNotification(id: string): void {
    const notification = this.notificationService.notifications()
      .find(item => item.id === id);

    if (!notification) {
      return;
    }

    this.notificationService.markAsRead(id);
    this.closeNotifications();

    if (notification.type === 'client') {
      if (notification.title.includes('deleted')) {
        this.router.navigateByUrl('/clients');
      } else {
        this.router.navigate(['/clients', notification.entityId]);
      }
    } else if (notification.type === 'followup') {
      this.router.navigateByUrl('/follow-ups');
    } else if (notification.type === 'note') {
      this.router.navigate(['/clients', notification.entityId]);
    }
  }

}
