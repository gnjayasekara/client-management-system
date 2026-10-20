import { Component, inject, output, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-add-user-modal',
  imports: [FormsModule],
  templateUrl: './add-user-modal.html'
})
export class AddUserModal implements OnInit {
  private readonly authService = inject(AuthService);

  readonly close = output<void>();
  readonly userCreated = output<void>();

  readonly creatableRoles = this.authService.creatableRoles;
  readonly loading = signal(false);
  readonly errorMessage = signal('');
  readonly successMessage = signal('');

  username = '';
  email = '';
  password = '';
  selectedRole = '';

  ngOnInit(): void {
    const roles = this.creatableRoles();
    if (roles.length > 0) {
      this.selectedRole = roles[0];
    }
  }

  submit(): void {
    if (!this.username.trim() || !this.email.trim() || !this.password || !this.selectedRole) {
      return;
    }

    this.loading.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    this.authService
      .registerUser({
        username: this.username.trim(),
        email: this.email.trim(),
        password: this.password,
        role: this.selectedRole
      })
      .subscribe({
        next: (res) => {
          this.loading.set(false);
          this.successMessage.set(`User ${res.username} (${res.role}) created successfully!`);
          this.userCreated.emit();
          setTimeout(() => {
            this.close.emit();
          }, 1500);
        },
        error: (err: HttpErrorResponse) => {
          this.loading.set(false);
          const msg = err.error?.message || 'Failed to create user. Ensure password meets security standards.';
          this.errorMessage.set(msg);
        }
      });
  }
}
