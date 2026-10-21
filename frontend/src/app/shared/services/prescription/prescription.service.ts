import { inject, Service } from '@angular/core';

import { map } from 'rxjs';

import { ApiService } from '@core/services/api/api.service';

import { PagedResult } from '@core/types/api.types';
import { PrescriptionItem, PrescriptionRequest } from './prescription.types';

@Service()
export class PrescriptionService {
  apiService = inject(ApiService);

  /**
   * Every line of the visit's prescription. An empty array means none was
   * written; the API answers 200 either way, unlike the diagnosis which 404s.
   *
   * The response keeps the paged envelope every list uses, but this endpoint
   * never truncates: the update replaces the whole set, so a partial read
   * would make a save delete the rest.
   *
   * @param visitId The visit.
   */
  getPrescription = (visitId: number) => this.apiService
    .get<PagedResult<PrescriptionItem>>(`/Visits/${visitId}/prescription`)
    .pipe(map((page) => page.items));

  /**
   * The same prescription as reactive state, for a screen that only reads it.
   *
   * @param visitId Returns the visit.
   * @returns The resource holding the lines.
   */
  getPrescriptionResource = (visitId: () => number) => this.apiService.getPagedResource<PrescriptionItem>(() => `/Visits/${visitId()}/prescription`);

  /**
   * Write the visit's first prescription. Answers 409 if one already exists.
   *
   * @param visitId The visit.
   * @param body Every line, at least one.
   */
  createPrescription = (visitId: number, body: PrescriptionRequest) => this.apiService.post(`/Visits/${visitId}/prescription`, body);

  /**
   * Replace the whole prescription. The API runs a MERGE scoped to the visit,
   * so a medicine left out of the body is deleted. There is no way to send an
   * empty set, which is why nothing here clears a prescription.
   *
   * @param visitId The visit.
   * @param body Every line that should survive, at least one.
   */
  updatePrescription = (visitId: number, body: PrescriptionRequest) => this.apiService.put(`/Visits/${visitId}/prescription`, body);
}
