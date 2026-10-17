import { inject, Service } from '@angular/core';

import { ApiService } from '@core/services/api/api.service';

import { Profile, UpdateProfileRequest } from './profile.types';

@Service()
export class ProfileService {
  apiService = inject(ApiService);

  /**
   * The signed-in user's own record. No id is sent: the API reads it from the
   * token, which is the only thing that knows which person a login belongs to.
   */
  getProfile = () => this.apiService.get<Profile>('/Profile');

  /**
   * Correct your own details. The national number, the email and the role are
   * fixed elsewhere and cannot be changed here.
   *
   * @param body The replacement details.
   */
  updateProfile = (body: UpdateProfileRequest) => this.apiService.put('/Profile', body);
}
