import { computed, inject, Service, signal } from '@angular/core';

import { tap } from 'rxjs';

import { ApiService } from '@core/services/api/api.service';
import { LocalStorageService } from '@core/services/local-storage/local-storage.service';

import { LoginRequestBody, Role, TokenResponse } from '@core/auth/types/auth.types';

@Service()
export class AuthService {
  apiService = inject(ApiService);
  localStorage = inject(LocalStorageService);

  role = signal<Role | null>(null);

  constructor() {
    this.role.set(this.readRole());
  }

  isAdmin = computed(() => this.role() === 'Admin');
  isFrontDesk = computed(() => this.role() === 'Admin' || this.role() === 'Staff');
  canManageVisitRecord = computed(() => this.role() === 'Admin' || this.role() === 'Doctor');

  /**
   * Decodes the access token's payload and pulls the role out of it.
   *
   * The two replaceAll calls turn base64url back into standard base64, which
   * is the only one atob takes. .NET writes claims under schema URIs, so there
   * is no plain `role` key to read.
   *
   * @returns The role claim, or null when there is no usable token.
   */
  readRole(): Role | null {
    try {
      const payload = this.getAuthToken().split('.')[1].replaceAll('-', '+').replaceAll('_', '/');
      const claims = JSON.parse(atob(payload));

      return claims['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ?? null;
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

    this.role.set(this.readRole());
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

        this.role.set(this.readRole());
      })
    );
  }

  /**
   * Clear Auth related local storage fields
   */
  logout = () => {
    this.localStorage.remove("accessToken");
    this.localStorage.remove("refreshToken");
    this.localStorage.remove("email");

    this.role.set(null);
  }
}
