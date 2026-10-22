
import { Component, inject } from '@angular/core';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';

import { Location } from '@angular/common';

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

  private readonly location = inject(Location);

  loading = false;
  success = false;
  errorMessage = '';

  readonly userId =
    this.route.snapshot.queryParamMap.get('userId') ?? '';

  readonly token =
    this.route.snapshot.queryParamMap.get('token') ?? '';

  form = this.fb.nonNullable.group({
    newPassword: [
      '',
      [
        Validators.required,
        Validators.minLength(8),
        Validators.pattern(
          /^(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).+$/
        )
      ]
    ],
    confirmPassword: ['', Validators.required]
  });

  constructor() {
    if (this.userId && this.token) {
      this.location.replaceState('/reset-password');
    }

    this.form.valueChanges.subscribe(() => {
      if (!this.loading) {
        this.errorMessage = '';
      }
    });
  }

  private decodeResetToken(encodedToken: string): string {
    const base64 = encodedToken
      .replace(/-/g, '+')
      .replace(/_/g, '/');

    const padded = base64.padEnd(
      Math.ceil(base64.length / 4) * 4,
      '='
    );

    const binary = atob(padded);

    const bytes = Uint8Array.from(
      binary,
      char => char.charCodeAt(0)
    );

    return new TextDecoder().decode(bytes);
  }

  submit(): void {
    if (this.loading || this.success) {
      return;
    }

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

    let decodedToken: string;

    try {
      decodedToken = this.decodeResetToken(this.token);
    } catch {
      this.errorMessage =
        'Invalid password reset link. Please request a new one.';
      return;
    }

    this.loading = true;
    this.errorMessage = '';

    this.authService.resetPassword({
      userId: this.userId,
      token: decodedToken,
      newPassword: passwords.newPassword,
      confirmPassword: passwords.confirmPassword
    })
      .pipe(finalize(() => {
        this.loading = false;
      }))
      .subscribe({
        next: () => {
          this.success = true;
        },
        error: (error) => {
          const errors = error.error?.errors;

          this.errorMessage =
            Array.isArray(errors) && errors.length > 0
              ? errors.join(', ')
              : error.error?.message ||
                'Unable to reset password. The link may have expired.';
        }
      });
  }
}
