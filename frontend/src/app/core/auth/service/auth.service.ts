import { computed, inject, Service, signal } from '@angular/core';

import { finalize, Observable, shareReplay, tap } from 'rxjs';

import { ApiService } from '@core/services/api/api.service';
import { LocalStorageService } from '@core/services/local-storage/local-storage.service';

import { ChangePasswordRequest, LoginRequestBody, Role, TokenResponse } from '@core/auth/types/auth.types';

@Service()
export class AuthService {
  apiService = inject(ApiService);
  localStorage = inject(LocalStorageService);

  _role = signal<Role | null>(null);
  _userId = signal<number | null>(null);

  readonly role = this._role.asReadonly();
  readonly userId = this._userId.asReadonly();

  inFlightRefresh$: Observable<TokenResponse | null> | null = null;

  constructor() {
    this.readToken();
  }

  isAdmin = computed(() => this.role() === 'Admin');
  isFrontDesk = computed(() => this.role() === 'Admin' || this.role() === 'Staff');
  canManageVisitRecord = computed(() => this.role() === 'Admin' || this.role() === 'Doctor');

  /**
   * Pulls what the UI needs out of the access token. Called wherever the token
   * changes, so the signals and the token can never disagree.
   */
  readToken(): void {
    const claims = this.decodeToken();

    this._role.set((claims?.['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] as Role) ?? null);
    this._userId.set(Number(claims?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier']) || null);
  }

  /**
   * Decodes the access token's payload.
   *
   * The two replaceAll calls turn base64url back into standard base64, which is
   * the only one atob takes. .NET writes claims under schema URIs, so there are
   * no plain `role` or `sub` keys to read.
   *
   * @returns The claims, or null when there is no usable token.
   */
  decodeToken(): Record<string, string> | null {
    try {
      const payload = this.getAuthToken().split('.')[1].replaceAll('-', '+').replaceAll('_', '/');

      return JSON.parse(atob(payload));
    } catch {
      return null;
    }
  }

  /**
   * Get Access Token
   */
  getAuthToken = () => this.localStorage.get("accessToken") ?? "";

  /**
   * Get Refresh Token
   */
  getRefreshToken = () => this.localStorage.get("refreshToken") ?? "";

  /**
   * Get Email
   */
  getEmail = () => this.localStorage.get("email") ?? "";

  /**
   * Make post request to login endpoint
   *
   * @param body
   */
  login = (body: LoginRequestBody) => this.apiService.post<TokenResponse>('/Auth/login', body).pipe(tap(response => {
    if (!response)
      return;

    this.localStorage.set("accessToken", response.accessToken);
    this.localStorage.set("refreshToken", response.refreshToken);
    this.localStorage.set("email", body.email);

    this.readToken();
  }));

  /**
   * Rotate the access / refresh tokens using user email and existing refresh token in local storage.
   */
  refresh = () => {
    return this.apiService.post<TokenResponse>("/Auth/refresh", {
      email: this.getEmail(),
      refreshToken: this.getRefreshToken(),
    }).pipe(
      tap(response => {
        if (!response)
          return;

        this.localStorage.set("accessToken", response.accessToken);
        this.localStorage.set("refreshToken", response.refreshToken);

        this.readToken();
      })
    );
  }

  /**
   * Rotates the tokens, but only once no matter how many callers ask.
   *
   * A page that fires five requests at an expired token gets five 401s at the
   * same moment. Without sharing, five refreshes race, four of them send a
   * token the first has already rotated away, and the user is signed out.
   *
   * @returns The in-flight refresh, shared until it settles.
   */
  refreshOnce = (): Observable<TokenResponse | null> => {
    if (!this.inFlightRefresh$) {
      this.inFlightRefresh$ = this.refresh().pipe(
        finalize(() => this.inFlightRefresh$ = null),
        shareReplay({ bufferSize: 1, refCount: false }),
      );
    }

    return this.inFlightRefresh$;
  }

  /**
   * Replace an account's password.
   *
   * Changing your own means proving you know the current one. An admin
   * resetting a doctor's or a staff member's does not send it, because the
   * whole point is that nobody knows it.
   *
   * Every refresh token on that account dies with the old password, so a reset
   * signs the owner out everywhere.
   *
   * @param userId The account.
   * @param body The current password when it is your own, and the new one.
   */
  changePassword = (userId: number, body: ChangePasswordRequest) => this.apiService.patch(`/Auth/${userId}/change-password`, body);

  /**
   * Clear Auth related local storage fields
   */
  logout = () => {
    this.localStorage.remove("accessToken");
    this.localStorage.remove("refreshToken");
    this.localStorage.remove("email");

    this.readToken();
  }
}
