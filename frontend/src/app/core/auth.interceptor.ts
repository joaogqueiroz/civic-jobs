import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

/**
 * The browser sends the httpOnly session cookie by itself (same origin through the proxy), so no header is added.
 * This only ends the session when the API says it is no longer valid.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);

  return next(request).pipe(
    catchError((error: unknown) => {
      // A 401 from login means wrong credentials, and /users/me at startup just means "not logged in".
      const isSessionCheck =
        request.url.startsWith('/identity/api/auth/') ||
        (request.url === '/identity/api/users/me' && !auth.isAuthenticated());

      if (error instanceof HttpErrorResponse && error.status === 401 && !isSessionCheck) {
        auth.endSession();
      }

      return throwError(() => error);
    }),
  );
};
