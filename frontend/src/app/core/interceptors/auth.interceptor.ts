
import { inject } from '@angular/core';
import {
  HttpInterceptorFn,
  HttpErrorResponse
} from '@angular/common/http';

import { catchError, throwError } from 'rxjs';

import { AuthService } from '../services/auth.service';
import { environment } from '../../../environments/environment';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);

  const apiBase = environment.apiUrl.replace(/\/$/, '');

  const isOurApi =
    req.url === apiBase ||
    req.url.startsWith(`${apiBase}/`);

  const isLoginRequest =
    req.url === `${apiBase}/auth/login`;

  const token = authService.getToken();

  const request = isOurApi && !isLoginRequest && token
    ? req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      })
    : req;

  return next(request).pipe(
    catchError((error: HttpErrorResponse) => {
      if (
        isOurApi &&
        !isLoginRequest &&
        error.status === 401 &&
        token
      ) {
        authService.logout();
      }

      return throwError(() => error);
    })
  );
};
