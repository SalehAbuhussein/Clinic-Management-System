import { inject, Service } from '@angular/core';

import { ApiService } from '@core/services/api/api.service';

import { Diagnosis, DiagnosisRequest } from './diagnosis.types';

@Service()
export class DiagnosisService {
  apiService = inject(ApiService);

  /**
   * The visit's diagnosis. A visit has at most one, so there is no id in the
   * path. Answers 404 when the doctor has not written one yet, which is an
   * ordinary state rather than a failure.
   *
   * @param visitId The visit.
   */
  getDiagnosis = (visitId: number) => this.apiService.get<Diagnosis>(`/Visits/${visitId}/diagnosis`);

  /**
   * The same diagnosis as reactive state, for a screen that only reads it.
   *
   * @param visitId Returns the visit.
   * @returns The resource holding the diagnosis.
   */
  getDiagnosisResource = (visitId: () => number) => this.apiService.getResource<Diagnosis>(() => `/Visits/${visitId()}/diagnosis`);

  /**
   * Write the visit's first diagnosis. Answers 409 if one already exists.
   *
   * @param visitId The visit.
   * @param body The diagnosis and its notes.
   */
  createDiagnosis = (visitId: number, body: DiagnosisRequest) => this.apiService.post<Diagnosis>(`/Visits/${visitId}/diagnosis`, body);

  /**
   * Correct the visit's diagnosis. Refused once the visit is checked out.
   *
   * @param visitId The visit.
   * @param body The diagnosis and its notes.
   */
  updateDiagnosis = (visitId: number, body: DiagnosisRequest) => this.apiService.put(`/Visits/${visitId}/diagnosis`, body);
}
