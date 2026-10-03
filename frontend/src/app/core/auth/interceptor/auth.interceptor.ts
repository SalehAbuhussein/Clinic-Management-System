import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';

import { AuthService } from '@core/auth/service/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const accessToken = inject(AuthService).getAuthToken();

  if (accessToken) {
    const newReq = req.clone({
      setHeaders: {
        Authorization: `Bearer ${accessToken}`,
      }
    });

    return next(newReq);
  }

  return next(req);
};
