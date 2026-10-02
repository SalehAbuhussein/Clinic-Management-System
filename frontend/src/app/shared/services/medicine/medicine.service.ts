import { inject, Service } from '@angular/core';

import { ApiService } from '@core/services/api/api.service';

import { Medicine, MedicineFilters } from './medicine.types';

@Service()
export class MedicineService {
  apiService = inject(ApiService);

  /**
   * The medicine catalogue as reactive state. Read-only: the API has no write
   * verbs, because a prescription points at a medicine by id and editing one
   * would rewrite what every past prescription says.
   *
   * @param filters Returns the query parameters; `name` matches either name.
   * @returns The resource holding the page.
   */
  getMedicineResource = (filters: () => MedicineFilters = () => ({})) => this.apiService.getPagedResource<Medicine>('/Medicines', filters);
}
