import { inject, Service } from '@angular/core';

import { ApiService } from '@core/services/api/api.service';

import { PrescriptionItem, PrescriptionRequest } from './prescription.types';

@Service()
export class PrescriptionService {
  apiService = inject(ApiService);

  /**
   * The visit's prescription lines. An empty array means none was written; the
   * API answers 200 either way, unlike the diagnosis which 404s.
   *
   * @param visitId The visit.
   */
  getPrescription = (visitId: number) => this.apiService.get<PrescriptionItem[]>(`/Visits/${visitId}/prescription`);

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
