import { inject, Service } from '@angular/core';

import { ApiService } from '@core/services/api/api.service';

import { Person, PersonFilters, UpdatePersonRequest } from './person.types';

@Service()
export class PersonService {
  apiService = inject(ApiService);

  /**
   * Everyone on file, as reactive state.
   *
   * @param filters Returns the query parameters.
   * @returns The resource holding the page.
   */
  getPersonResource = (filters: () => PersonFilters = () => ({})) => {
    return this.apiService.getPagedResource<Person>('/People', filters);
  }

  /**
   * Correct someone's details. The API refuses another admin's.
   *
   * @param id The person.
   * @param body The replacement details. The national number cannot change.
   */
  updatePerson = (id: number, body: UpdatePersonRequest) => {
    return this.apiService.put(`/People/${id}`, body);
  }

  /**
   * Replaces someone's picture. The previous file is deleted once the new name
   * is recorded, so a person never keeps more than one.
   *
   * @param id The person.
   * @param image The chosen file. The API reads the type from the bytes and
   * refuses anything that is not a JPEG, PNG or WebP.
   */
  uploadImage = (id: number, image: File) => {
    const body = new FormData();
    body.append('image', image);

    return this.apiService.post<void>(`/People/${id}/image`, body);
  }

  /**
   * Clears someone's picture, and deletes the stored file.
   *
   * @param id The person.
   */
  removeImage = (id: number) => {
    return this.apiService.delete(`/People/${id}/image`);
  }

  /**
   * Remove a person. The API refuses one that a user or a patient belongs to,
   * which is almost all of them: a person is only ever created as part of a role.
   *
   * @param id The person.
   */
  deletePerson = (id: number) => {
    return this.apiService.delete(`/People/${id}`);
  }
}
