import { inject, Service } from '@angular/core';

import { map } from 'rxjs';

import { ApiService } from '@core/services/api/api.service';

import { PagedResult } from '@core/types/api.types';
import { PrescriptionItem, PrescriptionRequest } from './prescription.types';

@Service()
export class PrescriptionService {
  apiService = inject(ApiService);

  /**
   * The visit's prescription lines. An empty array means none was written; the
   * API answers 200 either way, unlike the diagnosis which 404s.
   *
   * The endpoint pages like every other list, but a prescription is read and
   * edited whole, so the envelope is unwrapped here rather than at each screen.
   * Its `perPage` caps at 10, so a longer prescription arrives truncated.
   *
   * @param visitId The visit.
   */
  getPrescription = (visitId: number) => this.apiService
    .get<PagedResult<PrescriptionItem>>(`/Visits/${visitId}/prescription`)
    .pipe(map((page) => page.items));

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
