import { inject, Service } from '@angular/core';

import { tap } from 'rxjs';

import { ApiService } from '@core/services/api/api.service';
import { LocalStorageService } from '@core/services/local-storage/local-storage.service';

import { LoginRequestBody, TokenResponse } from '../types/auth.types';

@Service()
export class AuthService {
  apiService = inject(ApiService);
  localStorage = inject(LocalStorageService);

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
      })
    );
  }

  /**
   * Clear Auth related local storage fields
   */
  clear = () => {
    this.localStorage.remove("accessToken");
    this.localStorage.remove("refreshToken");
    this.localStorage.remove("email");
  }
}
