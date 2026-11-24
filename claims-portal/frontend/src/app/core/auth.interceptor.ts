import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const token = localStorage.getItem('claimsportal.token');
  const authorized = token
    ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : request;

  const auth = inject(AuthService);
  const router = inject(Router);

  return next(authorized).pipe(
    catchError(error => {
      if (error instanceof HttpErrorResponse && error.status === 401 && !request.url.includes('/api/auth/login')) {
        auth.logout();
        void router.navigate(['/login']);
      }

      return throwError(() => error);
    })
  );
};
