import { inject, Service } from '@angular/core';
import { Params } from '@angular/router';

import { ApiService } from '@core/services/api/api.service';
import { LoginRequestBody } from '../types/login.types';

@Service()
export class LoginService {
  apiService = inject(ApiService);

  /**
   * Make post request to login endpoint
   *
   * @param body
   */
  postLogin = (body: LoginRequestBody) => {
    return this.apiService.post('/login', body);
  }
}
