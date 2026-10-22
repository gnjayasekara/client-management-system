import { TestBed, ComponentFixture } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { Login } from './login';
import { AuthService } from '../../../core/services/auth.service';
import { LoginResponse } from '../../../core/models/auth.model';

describe('Login Component', () => {
  let component: Login;
  let fixture: ComponentFixture<Login>;
  let authServiceSpy: { login: ReturnType<typeof vi.fn> };
  let router: Router;

  const mockLoginResponse: LoginResponse = {
    accessToken: 'mock-token',
    tokenType: 'Bearer',
    refreshToken: 'mock-refresh',
    expiresAt: new Date(Date.now() + 3600000).toISOString(),
    username: 'admin',
    roles: ['Admin'],
    permissions: [],
    mustChangePassword: false
  };

  beforeEach(async () => {
    authServiceSpy = {
      login: vi.fn()
    };

    await TestBed.configureTestingModule({
      imports: [Login],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authServiceSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(Login);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    fixture.detectChanges();
  });

  it('should create the login component', () => {
    expect(component).toBeTruthy();
    expect(component.loading()).toBe(false);
    expect(component.errorMessage()).toBe('');
  });

  it('should show error message when submitting empty fields', () => {
    component.credentials = { username: '', password: '' };
    component.onSubmit();

    expect(component.errorMessage()).toBe('Please enter your username and password.');
    expect(authServiceSpy.login).not.toHaveBeenCalled();
  });

  it('should navigate to /dashboard on successful login', () => {
    const navigateSpy = vi.spyOn(router, 'navigateByUrl');
    authServiceSpy.login.mockReturnValue(of(mockLoginResponse));

    component.credentials = { username: 'admin', password: 'secretpassword' };
    component.onSubmit();

    expect(authServiceSpy.login).toHaveBeenCalledWith({
      username: 'admin',
      password: 'secretpassword'
    });
    expect(component.loading()).toBe(false);
    expect(component.errorMessage()).toBe('');
    expect(navigateSpy).toHaveBeenCalledWith('/dashboard');
  });

  it('should navigate to /change-password if mustChangePassword is true', () => {
    const navigateSpy = vi.spyOn(router, 'navigateByUrl');
    authServiceSpy.login.mockReturnValue(of({ ...mockLoginResponse, mustChangePassword: true }));

    component.credentials = { username: 'admin', password: 'secretpassword' };
    component.onSubmit();

    expect(navigateSpy).toHaveBeenCalledWith('/change-password');
  });

  it('should show invalid credentials on 401 error', () => {
    const errorResponse = new HttpErrorResponse({ status: 401, statusText: 'Unauthorized' });
    authServiceSpy.login.mockReturnValue(throwError(() => errorResponse));

    component.credentials = { username: 'wrong', password: 'password' };
    component.onSubmit();

    expect(component.loading()).toBe(false);
    expect(component.errorMessage()).toBe('Invalid username or password.');
  });

  it('should show email verification message on 403 EMAIL_NOT_VERIFIED error', () => {
    const errorResponse = new HttpErrorResponse({
      status: 403,
      error: { code: 'EMAIL_NOT_VERIFIED' }
    });
    authServiceSpy.login.mockReturnValue(throwError(() => errorResponse));

    component.credentials = { username: 'unverified', password: 'password' };
    component.onSubmit();

    expect(component.loading()).toBe(false);
    expect(component.errorMessage()).toContain('Please verify your email before logging in');
  });

  it('should show connection error on status 0', () => {
    const errorResponse = new HttpErrorResponse({ status: 0 });
    authServiceSpy.login.mockReturnValue(throwError(() => errorResponse));

    component.credentials = { username: 'user', password: 'password' };
    component.onSubmit();

    expect(component.loading()).toBe(false);
    expect(component.errorMessage()).toBe('Cannot connect to the backend server.');
  });
});
