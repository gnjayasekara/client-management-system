import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';
import { LoginResponse } from '../models/auth.model';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  let router: Router;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([])
      ]
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created with initial logged-out state', () => {
    expect(service).toBeTruthy();
    expect(service.username()).toBeNull();
    expect(service.roles()).toEqual([]);
    expect(service.permissions()).toEqual([]);
    expect(service.isSuperAdmin()).toBe(false);
    expect(service.isAdmin()).toBe(false);
    expect(service.isManager()).toBe(false);
    expect(service.canCreateUser()).toBe(false);
    expect(service.isAuthenticated()).toBe(false);
    expect(service.getToken()).toBeNull();
  });

  it('should authenticate user and set signals on successful login', () => {
    const mockResponse: LoginResponse = {
      accessToken: 'test-token-xyz',
      tokenType: 'Bearer',
      refreshToken: 'test-refresh-token',
      expiresAt: new Date(Date.now() + 3600 * 1000).toISOString(),
      username: 'johndoe',
      roles: ['Admin'],
      permissions: ['clients.view', 'clients.create'],
      mustChangePassword: false
    };

    service.login({ username: 'johndoe', password: 'password123' }).subscribe((res) => {
      expect(res).toEqual(mockResponse);
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/auth/login`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ username: 'johndoe', password: 'password123' });

    req.flush(mockResponse);

    expect(service.username()).toBe('johndoe');
    expect(service.roles()).toEqual(['Admin']);
    expect(service.permissions()).toEqual(['clients.view', 'clients.create']);
    expect(service.isAdmin()).toBe(true);
    expect(service.isSuperAdmin()).toBe(false);
    expect(service.canCreateUser()).toBe(true);
    expect(service.creatableRoles()).toEqual(['Manager', 'Staff']);
    expect(service.isAuthenticated()).toBe(true);
    expect(service.getToken()).toBe('test-token-xyz');
    expect(service.hasRole('Admin')).toBe(true);
    expect(service.hasPermission('clients.view')).toBe(true);
  });

  it('should clear session and navigate to /login on logout', () => {
    const navigateSpy = vi.spyOn(router, 'navigateByUrl');

    // Manually set logged in state
    service.username.set('testuser');
    service.roles.set(['Manager']);

    service.logout();

    expect(service.username()).toBeNull();
    expect(service.roles()).toEqual([]);
    expect(service.isAuthenticated()).toBe(false);
    expect(navigateSpy).toHaveBeenCalledWith('/login');
  });

  it('should correctly determine creatableRoles for SuperAdmin', () => {
    service.roles.set(['SuperAdmin']);
    expect(service.isSuperAdmin()).toBe(true);
    expect(service.canCreateUser()).toBe(true);
    expect(service.creatableRoles()).toEqual(['Admin', 'Manager', 'Staff']);
  });

  it('should correctly determine creatableRoles for Manager', () => {
    service.roles.set(['Manager']);
    expect(service.isManager()).toBe(true);
    expect(service.canCreateUser()).toBe(true);
    expect(service.creatableRoles()).toEqual(['Staff']);
  });

  it('should send forgotPassword request correctly', () => {
    const mockMsg = { message: 'Password reset link sent.' };
    service.forgotPassword('test@example.com').subscribe((res) => {
      expect(res).toEqual(mockMsg);
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/auth/forgot-password`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email: 'test@example.com' });
    req.flush(mockMsg);
  });
});
