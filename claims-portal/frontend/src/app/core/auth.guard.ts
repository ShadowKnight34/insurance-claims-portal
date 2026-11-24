import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = route => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isLoggedIn()) {
    return router.createUrlTree(['/login']);
  }

  const role = route.data['role'] as string | undefined;
  if (role && auth.user()?.role !== role) {
    return router.createUrlTree([auth.home()]);
  }

  return true;
};
