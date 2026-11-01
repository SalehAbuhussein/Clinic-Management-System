import { inject, Service } from '@angular/core';

import { ApiService } from '@core/services/api/api.service';

import { CreateUserRequest, UpdateUserRequest, User, UserFilters } from './user.types';

@Service()
export class UserService {
  apiService = inject(ApiService);

  /**
   * The staff list as reactive state.
   *
   * @param filters Returns the query parameters.
   * @returns The resource holding the page.
   */
  getUserResource = (filters: () => UserFilters = () => ({})) => {
    return this.apiService.getPagedResource<User>('/Users', filters);
  }

  /**
   * Create an account. It starts as Staff whatever the clinic means it to be,
   * because the database sets the role and the create call never sends one.
   *
   * @param body The person and their credentials.
   */
  createUser = (body: CreateUserRequest) => {
    return this.apiService.post<void>('/Users', body);
  }

  /**
   * Change an account's email or role.
   *
   * @param id The user.
   * @param body The new email and role.
   */
  updateUser = (id: number, body: UpdateUserRequest) => {
    return this.apiService.put(`/Users/${id}`, body);
  }

  /**
   * Switch an account off. The API refuses if it is the caller's own, because
   * an admin locking themselves out needs another admin to undo it.
   *
   * @param id The user.
   */
  deactivateUser = (id: number) => {
    return this.apiService.patch(`/Users/${id}/deactivate`);
  }

  /**
   * Switch an account back on.
   *
   * @param id The user.
   */
  activateUser = (id: number) => {
    return this.apiService.patch(`/Users/${id}/activate`);
  }
}
