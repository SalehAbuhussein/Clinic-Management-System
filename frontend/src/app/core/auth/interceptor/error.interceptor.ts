import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';

import { catchError, throwError } from 'rxjs';

import { ApiError } from '../types/auth.types';

const MESSAGE_BY_STATUS: Record<number, string> = {
  401: 'Your session has ended. Sign in again.',
  403: 'You do not have permission to do that.',
  404: 'That record no longer exists.',
  500: 'Something went wrong on the server. Try again.',
};

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  return next(req).pipe(
    catchError((response: HttpErrorResponse) => {
      const problem = response.error;
      const apiError: ApiError = {
        status: response.status,
        message: problem?.detail ?? MESSAGE_BY_STATUS[response.status] ?? "Something went wrong.",
        traceId: problem?.traceId ?? null,
      };

      console.log(apiError);

      return throwError(() => apiError);
    })
  );
};
