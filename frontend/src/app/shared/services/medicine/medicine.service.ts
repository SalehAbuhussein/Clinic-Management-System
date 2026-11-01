import { inject, Service } from '@angular/core';

import { ApiService } from '@core/services/api/api.service';

import { CreateMedicineRequest, Medicine, MedicineFilters, UpdateMedicineRequest } from './medicine.types';

@Service()
export class MedicineService {
  apiService = inject(ApiService);

  /**
   * The medicine catalogue as reactive state. Every role may read it; only an
   * admin may change it.
   *
   * @param filters Returns the query parameters; `name` matches either name.
   * @returns The resource holding the page.
   */
  getMedicineResource = (filters: () => MedicineFilters = () => ({})) => {
    return this.apiService.getPagedResource<Medicine>('/Medicines', filters);
  }

  /**
   * Medicines matching what the user is typing, for the prescription
   * autocomplete. No term means no request, so opening the list does not pull
   * the whole catalogue.
   *
   * @param term The search text, already debounced and trimmed.
   * @returns The resource holding the matches.
   */
  searchMedicineResource = (term: () => string) => {
    return this.apiService.getPagedResource<Medicine>(() => (term() ? '/Medicines' : null), () => ({ name: term() }));
  }

  /**
   * Add a medicine so it can be prescribed.
   *
   * @param body The RxCUI, the names, the form and the strength.
   */
  createMedicine = (body: CreateMedicineRequest) => {
    return this.apiService.post<Medicine>('/Medicines', body);
  }

  /**
   * Correct a medicine. Prescriptions name a medicine by id and read its
   * details through a join, so an edit changes what every past prescription
   * shows. Use it to fix a typo, not to repurpose a row.
   *
   * @param id The medicine.
   * @param body The replacement details. The RxCUI cannot change.
   */
  updateMedicine = (id: number, body: UpdateMedicineRequest) => {
    return this.apiService.put(`/Medicines/${id}`, body);
  }

  /**
   * Remove a medicine. The API refuses one that any prescription names.
   *
   * @param id The medicine.
   */
  deleteMedicine = (id: number) => {
    return this.apiService.delete(`/Medicines/${id}`);
  }
}
