import { inject, Service } from '@angular/core';

import { ApiService } from '@core/services/api/api.service';

import { LoginRequestBody } from '@features/login/types/login.types';
import { TokenResponse } from '../types/auth.types';

@Service()
export class AuthService {
  apiService = inject(ApiService);

  /**
   * Make post request to login endpoint
   *
   * @param body
   */
  login = (body: LoginRequestBody) => {
    return this.apiService.post<TokenResponse>('/Auth/login/', body);
  }
}
