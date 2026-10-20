
import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-confirm-email',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './confirm-email.html'
})
export class ConfirmEmail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly authService = inject(AuthService);

  loading = true;
  success = false;
  message = 'Verifying your email address...';

  ngOnInit(): void {
    const userId =
      this.route.snapshot.queryParamMap.get('userId');

    const token =
      this.route.snapshot.queryParamMap.get('token');

    if (!userId || !token) {
      this.loading = false;
      this.message = 'Invalid verification link.';
      return;
    }

    this.authService.confirmEmail({ userId, token })
      .pipe(finalize(() => this.loading = false))
      .subscribe({
        next: (response) => {
          this.success = true;
          this.message =
            response.message || 'Email verified successfully.';
        },
        error: (error) => {
          this.message =
            error.error?.message ||
            'Verification failed. The link may be invalid or expired.';
        }
      });
  }
}
