
import { Component, inject, input, output } from '@angular/core';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-header',
  templateUrl: './header.html',
  styleUrl: './header.css'
})
export class Header {
  private readonly authService = inject(AuthService);

  readonly title = input<string>('Dashboard');
  readonly menuClicked = output<void>();

  readonly username = this.authService.username;
}
