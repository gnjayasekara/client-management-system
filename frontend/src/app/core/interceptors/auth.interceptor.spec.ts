import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from '../services/auth.service';
import { environment } from '../../../environments/environment';

describe('authInterceptor', () => {
  let httpClient: HttpClient;
  let httpMock: HttpTestingController;
  let authServiceSpy: { getToken: ReturnType<typeof vi.fn>; logout: ReturnType<typeof vi.fn> };

  const apiBase = environment.apiUrl.replace(/\/$/, '');

  beforeEach(() => {
    authServiceSpy = {
      getToken: vi.fn(),
      logout: vi.fn()
    };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: authServiceSpy }
      ]
    });

    httpClient = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should attach Authorization header for API requests when token exists', () => {
    authServiceSpy.getToken.mockReturnValue('valid-jwt-token');

    httpClient.get(`${apiBase}/clients`).subscribe();

    const req = httpMock.expectOne(`${apiBase}/clients`);
    expect(req.request.headers.has('Authorization')).toBe(true);
    expect(req.request.headers.get('Authorization')).toBe('Bearer valid-jwt-token');
    req.flush([]);
  });

  it('should not attach Authorization header for login request', () => {
    authServiceSpy.getToken.mockReturnValue('valid-jwt-token');

    httpClient.post(`${apiBase}/auth/login`, {}).subscribe();

    const req = httpMock.expectOne(`${apiBase}/auth/login`);
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});
  });

  it('should call logout on 401 response for authenticated API call', () => {
    authServiceSpy.getToken.mockReturnValue('valid-jwt-token');

    httpClient.get(`${apiBase}/clients`).subscribe({
      error: (err) => {
        expect(err.status).toBe(401);
      }
    });

    const req = httpMock.expectOne(`${apiBase}/clients`);
    req.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });

    expect(authServiceSpy.logout).toHaveBeenCalledTimes(1);
  });
});
