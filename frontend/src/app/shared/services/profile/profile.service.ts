import { inject, Service } from '@angular/core';

import { ApiService } from '@core/services/api/api.service';

import { Profile, UpdateProfileRequest } from './profile.types';

@Service()
export class ProfileService {
  apiService = inject(ApiService);

  /**
   * An account's record. The API allows your own, and an admin may also read a
   * doctor's or a staff member's, but never another admin's.
   *
   * @param userId The account.
   */
  getProfile = (userId: number) => {
    return this.apiService.get<Profile>(`/Profile/${userId}`);
  }

  /**
   * Correct an account's details. The API allows your own, and an admin may
   * also correct a doctor's or a staff member's, but never another admin's.
   *
   * The national number, the email and the role are fixed elsewhere and
   * cannot be changed here.
   *
   * @param userId The account, which `getProfile` returns for your own.
   * @param body The replacement details.
   */
  updateProfile = (userId: number, body: UpdateProfileRequest) => {
    return this.apiService.put(`/Profile/${userId}`, body);
  }
}
