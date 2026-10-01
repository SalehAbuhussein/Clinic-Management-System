import { inject, Service } from '@angular/core';

import { ApiService } from '@core/services/api/api.service';

import { Visit } from './visit.types';

@Service()
export class VisitService {
  apiService = inject(ApiService);

  /**
   * Open a visit for an appointment. The API also flips the appointment to
   * Attended, so checking in is the whole arrival step and not half of it.
   *
   * The check-in time is the server's own clock. Sending the browser's would
   * mean a workstation running a few seconds fast fails every check-in against
   * a server that thinks the time is still in the future.
   *
   * @param appointmentId The appointment the patient arrived for.
   * @returns The new visit, carrying the id nothing else can look up.
   */
  checkIn = (appointmentId: number) => this.apiService.post<Visit>('/Visits', { appointmentId });
}
