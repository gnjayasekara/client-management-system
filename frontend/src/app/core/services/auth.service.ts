
import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import { LoginRequest, LoginResponse, RegisterUserRequest } from '../models/auth.model';


import {
  ApiMessage,
  ConfirmEmailRequest,
  ResetPasswordRequest,
  ChangePasswordRequest
} from '../models/auth.model';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  readonly mustChangePassword = signal(false);

  private accessToken: string | null = null;
  private expiresAt: number | null = null;

  readonly username = signal<string | null>(null);
  readonly roles = signal<string[]>([]);
  readonly permissions = signal<string[]>([]);
  readonly isSuperAdmin = computed(() => this.roles().includes('SuperAdmin'));
  readonly isAdmin = computed(() => this.roles().includes('Admin') || this.isSuperAdmin());
  readonly isManager = computed(() => this.roles().includes('Manager'));

  // Hierarchy check: who can create users?
  // SuperAdmin, Admin, and Manager can create users.
  readonly canCreateUser = computed(() =>
    this.isSuperAdmin() || this.roles().includes('Admin') || this.isManager()
  );

  // Computed allowable roles the current user is permitted to create
  readonly creatableRoles = computed<string[]>(() => {
    if (this.isSuperAdmin()) {
      return ['Admin', 'Manager', 'Staff'];
    }
    if (this.roles().includes('Admin')) {
      return ['Manager', 'Staff'];
    }
    if (this.isManager()) {
      return ['Staff'];
    }
    return [];
  });

  login(credentials: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(
      `${environment.apiUrl}/auth/login`,
      credentials
    ).pipe(
      tap(response => {
        this.accessToken = response.accessToken;
        this.expiresAt = new Date(response.expiresAt).getTime();
        this.username.set(response.username);
        this.roles.set(response.roles ?? []);
        this.permissions.set(response.permissions ?? []);
        this.mustChangePassword.set(
          response.mustChangePassword ?? false
        );
      })
    );
  }

  registerUser(userData: RegisterUserRequest): Observable<{ message: string; username: string; role: string }> {
    return this.http.post<{ message: string; username: string; role: string }>(
      `${environment.apiUrl}/auth/register`,
      userData
    );
  }

  
  confirmEmail(request: ConfirmEmailRequest): Observable<ApiMessage> {
    return this.http.post<ApiMessage>(
      `${environment.apiUrl}/auth/confirm-email`,
      request
    );
  }

  forgotPassword(email: string): Observable<ApiMessage> {
    return this.http.post<ApiMessage>(
      `${environment.apiUrl}/auth/forgot-password`,
      { email }
    );
  }

  resetPassword(
    request: ResetPasswordRequest
  ): Observable<ApiMessage> {
    return this.http.post<ApiMessage>(
      `${environment.apiUrl}/auth/reset-password`,
      request
    );
  }

  changePassword(
    request: ChangePasswordRequest
  ): Observable<ApiMessage> {
    return this.http.post<ApiMessage>(
      `${environment.apiUrl}/auth/change-password`,
      request
    );
  }


  hasRole(role: string): boolean {
    return this.roles().includes(role);
  }

  hasPermission(permission: string): boolean {
    return this.permissions().includes(permission);
  }

  getToken(): string | null {
    if (!this.isAuthenticated()) {
      return null;
    }

    return this.accessToken;
  }

  isAuthenticated(): boolean {
    const authenticated =
      this.accessToken !== null &&
      this.expiresAt !== null &&
      Number.isFinite(this.expiresAt) &&
      Date.now() < this.expiresAt;

    if (!authenticated) {
      this.clearSession();
    }

    return authenticated;
  }

  getAccessToken(): string | null {
    return this.isAuthenticated()
      ? this.accessToken
      : null;
  }

  logout(): void {
    this.clearSession();
    this.router.navigateByUrl('/login');
    this.mustChangePassword.set(false);
  }

  private clearSession(): void {
    this.accessToken = null;
    this.expiresAt = null;
    this.username.set(null);
    this.roles.set([]);
    this.permissions.set([]);
  }
}
