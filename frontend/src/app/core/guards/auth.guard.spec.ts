import { TestBed } from '@angular/core/testing';
import { Router, ActivatedRouteSnapshot, RouterStateSnapshot, UrlTree } from '@angular/router';
import { authGuard, authChildGuard } from './auth.guard';
import { AuthService } from '../services/auth.service';

describe('authGuard & authChildGuard', () => {
  let authServiceSpy: { isAuthenticated: ReturnType<typeof vi.fn>; mustChangePassword: { (): boolean; set?: (v: boolean) => void } };
  let router: Router;

  const mockRoute = {} as ActivatedRouteSnapshot;
  const createMockState = (url: string): RouterStateSnapshot => ({ url } as RouterStateSnapshot);

  beforeEach(() => {
    authServiceSpy = {
      isAuthenticated: vi.fn(),
      mustChangePassword: vi.fn(() => false) as any
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: authServiceSpy }
      ]
    });

    router = TestBed.inject(Router);
  });

  describe('authGuard', () => {
    it('should redirect unauthenticated users to /login', () => {
      authServiceSpy.isAuthenticated.mockReturnValue(false);

      const result = TestBed.runInInjectionContext(() =>
        authGuard(mockRoute, createMockState('/dashboard'))
      );

      expect(result instanceof UrlTree).toBe(true);
      expect((result as UrlTree).toString()).toBe('/login');
    });

    it('should redirect to /change-password if mustChangePassword is true and target is not change-password', () => {
      authServiceSpy.isAuthenticated.mockReturnValue(true);
      authServiceSpy.mustChangePassword = vi.fn(() => true) as any;

      const result = TestBed.runInInjectionContext(() =>
        authGuard(mockRoute, createMockState('/dashboard'))
      );

      expect(result instanceof UrlTree).toBe(true);
      expect((result as UrlTree).toString()).toBe('/change-password');
    });

    it('should allow access to /change-password even if mustChangePassword is true', () => {
      authServiceSpy.isAuthenticated.mockReturnValue(true);
      authServiceSpy.mustChangePassword = vi.fn(() => true) as any;

      const result = TestBed.runInInjectionContext(() =>
        authGuard(mockRoute, createMockState('/change-password'))
      );

      expect(result).toBe(true);
    });

    it('should allow access when authenticated and password change not required', () => {
      authServiceSpy.isAuthenticated.mockReturnValue(true);
      authServiceSpy.mustChangePassword = vi.fn(() => false) as any;

      const result = TestBed.runInInjectionContext(() =>
        authGuard(mockRoute, createMockState('/clients'))
      );

      expect(result).toBe(true);
    });
  });

  describe('authChildGuard', () => {
    it('should protect child routes equivalently', () => {
      authServiceSpy.isAuthenticated.mockReturnValue(false);

      const result = TestBed.runInInjectionContext(() =>
        authChildGuard(mockRoute, createMockState('/clients/123'))
      );

      expect(result instanceof UrlTree).toBe(true);
      expect((result as UrlTree).toString()).toBe('/login');
    });
  });
});
