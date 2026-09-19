import { inject, Service } from '@angular/core';

import { ApiService } from '@core/services/api/api.service';

import { GetPatientsResponse, PatientFilters } from './patient.types';

@Service()
export class PatientService {
  apiService = inject(ApiService);

  /**
   * Patients as reactive state, filtered however the caller wants.
   *
   * @param filters Returns the query parameters; defaults to no filter at all.
   * @returns The resource holding the patient list.
   */
  getPatientResource = (filters: () => PatientFilters = () => ({})) => this.apiService.getResource<GetPatientsResponse>('/Patients', filters);
}
