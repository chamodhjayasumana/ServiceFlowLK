import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);

  const withCreds = req.clone({ withCredentials: true });

  return next(withCreds).pipe(
    catchError((error: HttpErrorResponse) => {
      const isAuthProbe =
        req.url.includes('/auth/login') ||
        req.url.includes('/auth/csrf') ||
        req.url.includes('/me') ||
        req.url.includes('/tenants/register');

      if (error.status === 401 && auth.isAuthenticated() && !isAuthProbe) {
        void auth.logout();
      }

      return throwError(() => error);
    })
  );
};
