
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './reset-password.html'
})
export class ResetPassword {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly authService = inject(AuthService);

  loading = false;
  success = false;
  errorMessage = '';

  readonly userId =
    this.route.snapshot.queryParamMap.get('userId') ?? '';

  readonly token =
    this.route.snapshot.queryParamMap.get('token') ?? '';

  form = this.fb.nonNullable.group({
    newPassword: ['', [Validators.required, Validators.minLength(6)]],
    confirmPassword: ['', Validators.required]
  });

  submit(): void {
    if (!this.userId || !this.token) {
      this.errorMessage = 'Invalid password reset link.';
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const passwords = this.form.getRawValue();

    if (passwords.newPassword !== passwords.confirmPassword) {
      this.errorMessage = 'Passwords do not match.';
      return;
    }

    this.loading = true;
    this.errorMessage = '';

    this.authService.resetPassword({
      userId: this.userId,
      token: this.token,
      ...passwords
    })
      .pipe(finalize(() => this.loading = false))
      .subscribe({
        next: () => {
          this.success = true;
        },
        error: (error) => {
          this.errorMessage =
            error.error?.errors?.join(', ') ||
            error.error?.message ||
            'Unable to reset password. The link may have expired.';
        }
      });
  }
}
