
import { Component, inject, output } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-sidebar',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.css'
})
export class Sidebar {
  private readonly authService = inject(AuthService);

  readonly navigated = output<void>();

  logout(): void {
    this.navigated.emit();
    this.authService.logout();
  }

  closeMenu(): void {
    this.navigated.emit();
  }
}
