import { inject, Service } from '@angular/core';

import { ApiService } from '@core/services/api/api.service';

import { DoctorFilters, GetDoctorsResponse } from './doctor.types';

@Service()
export class DoctorService {
  apiService = inject(ApiService);

  /**
   * Doctors as reactive state, filtered however the caller wants.
   *
   * @param filters Returns the query parameters; defaults to no filter at all.
   * @returns The resource holding the doctor list.
   */
  getDoctorResource = (filters: () => DoctorFilters = () => ({})) => this.apiService.getResource<GetDoctorsResponse>('/Doctors', filters);
}
