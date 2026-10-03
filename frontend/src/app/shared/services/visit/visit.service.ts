import { inject, Service } from '@angular/core';

import { ApiService } from '@core/services/api/api.service';

import { dayRange } from '@core/utils/day.utils';

import { Visit } from './visit.types';

@Service()
export class VisitService {
  apiService = inject(ApiService);

  /**
   * One day's visits, newest check-in first. A doctor gets their own patients
   * whatever they ask for; the API ignores any doctor the caller names.
   *
   * @param day Returns the day to show; the time of day is ignored.
   * @param page Returns the page to show.
   * @returns The resource holding the page.
   */
  getVisitResource = (day: () => Date, page: () => number) => this.apiService.getPagedResource<Visit>('/Visits', () => ({ ...dayRange(day()), page: page() }));

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

  /**
   * Correct an open visit. Only the arrival time and the notes can change; the
   * appointment behind it is fixed. The API rejects a time in the future, or
   * one after the checkout.
   *
   * @param id The visit.
   * @param body The corrected arrival time and notes.
   */
  updateVisit = (id: number, body: { checkinTime: string, notes?: string }) => this.apiService.put(`/Visits/${id}`, body);

  /**
   * Close a visit. The API refuses until the visit has a diagnosis, which only
   * a doctor or an admin can add, so this fails by design on a visit the
   * doctor has not written up yet.
   *
   * @param id The visit.
   */
  checkoutVisit = (id: number) => this.apiService.patch(`/Visits/${id}/checkout`);

  /**
   * Remove a visit that should never have been opened. A checked-out visit is
   * finalized and the API refuses to delete it.
   *
   * @param id The visit.
   */
  deleteVisit = (id: number) => this.apiService.delete(`/Visits/${id}`);
}
