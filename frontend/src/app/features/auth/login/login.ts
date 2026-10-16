
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';

import { AuthService } from '../../../core/services/auth.service';
import { LoginRequest } from '../../../core/models/auth.model';

@Component({
  selector: 'app-login',
  imports: [FormsModule],
  templateUrl: './login.html',
  styleUrl: './login.css'
})
export class Login {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  credentials: LoginRequest = {
    username: '',
    password: ''
  };

  readonly loading = signal(false);
  readonly errorMessage = signal('');

  onSubmit(): void {
    if (this.loading()) {
      return;
    }

    if (!this.credentials.username.trim() ||
        !this.credentials.password) {
      this.errorMessage.set(
        'Please enter your username and password.'
      );
      return;
    }

    this.loading.set(true);
    this.errorMessage.set('');

    this.authService.login(this.credentials).subscribe({
      next: () => {
        this.loading.set(false);
        this.router.navigateByUrl('/dashboard');
      },
      error: (error: HttpErrorResponse) => {
        this.loading.set(false);

        if (error.status === 401) {
          this.errorMessage.set(
            'Invalid username or password.'
          );
        } else if (error.status === 403) {
          this.errorMessage.set(
            'Your account is not authorized to log in.'
          );
        } else if (error.status === 0) {
          this.errorMessage.set(
            'Cannot connect to the backend server.'
          );
        } else {
          this.errorMessage.set(
            'Login failed. Please try again.'
          );
        }
      }
    });
  }
}
