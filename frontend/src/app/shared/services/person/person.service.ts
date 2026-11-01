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
   * Remove a person. The API refuses one that a user or a patient belongs to,
   * which is almost all of them: a person is only ever created as part of a role.
   *
   * @param id The person.
   */
  deletePerson = (id: number) => {
    return this.apiService.delete(`/People/${id}`);
  }
}
