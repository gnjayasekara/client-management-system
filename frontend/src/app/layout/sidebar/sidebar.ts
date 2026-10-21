
import { Component, inject, output, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { AddUserModal } from '../../features/auth/add-user-modal/add-user-modal';
import { NotificationService } from '../../core/services/notification.service';

@Component({
  selector: 'app-sidebar',
  imports: [RouterLink, RouterLinkActive, AddUserModal],
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.css'
})
export class Sidebar {
  readonly authService = inject(AuthService);

  readonly navigated = output<void>();
  readonly showAddUserModal = signal(false);

  readonly notificationService = inject(NotificationService);

  openAddUser(): void {
    this.showAddUserModal.set(true);
  }

  closeAddUser(): void {
    this.showAddUserModal.set(false);
  }

  logout(): void {
    this.notificationService.clear();

    this.navigated.emit();
    this.authService.logout();
  }

  closeMenu(): void {
    this.navigated.emit();
  }
}
