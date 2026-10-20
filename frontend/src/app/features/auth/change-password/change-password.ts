
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';

import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-change-password',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './change-password.html'
})
export class ChangePassword {
  private readonly fb = inject(FormBuilder);
  readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  loading = false;
  errorMessage = '';
  successMessage = '';

  form = this.fb.nonNullable.group({
    currentPassword: ['', Validators.required],
    newPassword: ['', [Validators.required, Validators.minLength(6)]],
    confirmPassword: ['', Validators.required]
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const request = this.form.getRawValue();

    if (request.newPassword !== request.confirmPassword) {
      this.errorMessage = 'New passwords do not match.';
      return;
    }

    if (request.currentPassword === request.newPassword) {
      this.errorMessage = 'Choose a different password.';
      return;
    }

    this.loading = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.authService.changePassword(request)
      .pipe(finalize(() => this.loading = false))
      .subscribe({
        next: () => {
          this.successMessage =
            'Password changed successfully. Please log in again.';

          this.authService.logout();

          this.router.navigateByUrl('/login');
        },
        error: (error) => {
          this.errorMessage =
            error.error?.errors?.join(', ') ||
            error.error?.message ||
            'Unable to change password.';
        }
      });
  }
}
