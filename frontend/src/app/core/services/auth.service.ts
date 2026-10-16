
import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import { LoginRequest, LoginResponse } from '../models/auth.model';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private accessToken: string | null = null;
  private expiresAt: number | null = null;

  readonly username = signal<string | null>(null);

  login(credentials: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(
      `${environment.apiUrl}/auth/login`,
      credentials
    ).pipe(
      tap(response => {
        this.accessToken = response.accessToken;
        this.expiresAt = new Date(response.expiresAt).getTime();
        this.username.set(response.username);
      })
    );
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

  logout(): void {
    this.clearSession();
    this.router.navigateByUrl('/login');
  }

  private clearSession(): void {
    this.accessToken = null;
    this.expiresAt = null;
    this.username.set(null);
  }
}
