import { inject, Service } from '@angular/core';

import { ApiService } from '@core/services/api/api.service';

import { AuthResponse } from '@core/auth/types/auth.types';
import { LoginRequestBody } from '../types/login.types';

@Service()
export class LoginService {
  apiService = inject(ApiService);

  /**
   * Make post request to login endpoint
   *
   * @param body
   */
  login = (body: LoginRequestBody) => {
    return this.apiService.post<AuthResponse>('/Auth/login/', body);
  }
}
