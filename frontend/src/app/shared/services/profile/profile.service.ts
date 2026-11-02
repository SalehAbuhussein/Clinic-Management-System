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
   * An account's record as reactive state, for a screen that keeps showing it
   * rather than reading it once.
   *
   * @param userId Returns the account, or null while there is no signed-in one.
   */
  getProfileResource = (userId: () => number | null) => {
    return this.apiService.getResource<Profile>(() => {
      const id = userId();

      return id ? `/Profile/${id}` : null;
    });
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

  /**
   * Replaces an account's picture. The previous file is deleted once the new
   * name is recorded, so an account never keeps more than one.
   *
   * @param userId The account.
   * @param image The chosen file. The API reads the type from the bytes and
   * refuses anything that is not a JPEG, PNG or WebP.
   */
  uploadProfileImage = (userId: number, image: File) => {
    const body = new FormData();
    body.append('image', image);

    return this.apiService.post<void>(`/Profile/${userId}/image`, body);
  }

  /**
   * Clears an account's picture, and deletes the stored file.
   *
   * @param userId The account.
   */
  removeProfileImage = (userId: number) => {
    return this.apiService.delete(`/Profile/${userId}/image`);
  }

  /**
   * Where a stored picture is served from. The API answers with the bare file
   * name, because the column holds the name and the origin belongs to whoever
   * is showing it.
   *
   * @param fileName What the profile carries, which is null until one is uploaded.
   */
  imageUrlOf = (fileName: string | null) => fileName ? `/uploads/profiles/${fileName}` : null;
}
