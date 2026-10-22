
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './forgot-password.html'
})
export class ForgotPassword {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);

  loading = false;
  submitted = false;
  errorMessage = '';

  form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]]
  });

  

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.errorMessage = '';

    this.authService.forgotPassword(this.form.controls.email.value)
      .pipe(finalize(() => this.loading = false))
      .subscribe({
        next: () => {
          this.submitted = true;
        },
        error: () => {
          this.errorMessage =
            'Unable to process your request. Please try again.';
        }
      });
  }

  tryAgain(): void {
    this.submitted = false;
    this.errorMessage = '';
    this.form.reset();
  }
}
