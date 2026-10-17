import { HttpHandlerFn, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';

import { catchError, switchMap, throwError } from 'rxjs';

import { AuthService } from '@core/auth/service/auth.service';

import { ApiError } from '@core/auth/types/auth.types';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return next(signed(req, authService)).pipe(
    catchError((error: ApiError) => isExpiredToken(error, req, authService)
      ? retryWithNewToken(req, next, authService, router, error)
      : throwError(() => error)),
  );
};

/**
 * @returns The request carrying the current access token, or untouched when
 * there is none to send.
 */
const signed = (req: HttpRequest<unknown>, authService: AuthService) => {
  const accessToken = authService.getAuthToken();

  return accessToken
    ? req.clone({ setHeaders: { Authorization: `Bearer ${accessToken}` } })
    : req;
};

/**
 * Whether a failure is worth rotating the tokens over.
 *
 * Refreshing a rejected login or a rejected refresh would loop, and without a
 * refresh token there is nothing to rotate.
 */
const isExpiredToken = (error: ApiError, req: HttpRequest<unknown>, authService: AuthService) =>
  error.status === 401
  && !!authService.getRefreshToken()
  && !['/api/Auth/login', '/api/Auth/refresh'].some((route) => req.url.startsWith(route));

/**
 * Rotates the tokens and sends the request again. A refresh that fails means
 * the token is spent or revoked, so the session is over.
 */
const retryWithNewToken = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn,
  authService: AuthService,
  router: Router,
  error: ApiError,
) => authService.refreshOnce().pipe(
  switchMap(() => next(signed(req, authService))),
  catchError(() => {
    authService.logout();
    router.navigate(['/']);

    return throwError(() => error);
  }),
);
