import { inject, Service } from '@angular/core';

import { ApiService } from '@core/services/api/api.service';

import { AppointmentFilters, GetAppointmentsResponse, GetDoctorSchedulesResponse } from './appointment.types';

@Service()
export class AppointmentService {
  apiService = inject(ApiService);

  /**
   * Get Appointment List
   */
  getAppointmentList = () => this.apiService.get<GetAppointmentsResponse>('/Appointments');

  /**
   * Appointments as reactive state, filtered however the caller wants.
   *
   * The filters function is re-read on every refresh, so any signal read
   * inside it becomes a dependency: changing it refetches and cancels the
   * request already in flight.
   *
   * @param filters Returns the query parameters; defaults to no filter at all.
   * @returns The resource holding the appointment list.
   */
  getAppointmentResource = (filters: () => AppointmentFilters = () => ({})) => this.apiService.getResource<GetAppointmentsResponse>('/Appointments', filters);

  /**
   * Appointments for a single calendar day.
   *
   * A day is a range of one, so both bounds take the same value.
   *
   * @param day Returns the day as `yyyy-MM-dd` in local time, never an instant.
   * @returns The resource holding that day's appointments.
   */
  getTodayAppointmentResource = (day: () => string, page: () => number) => this.getAppointmentResource(() => ({ from: day(), to: day(), page: page() }));

  /**
   * The slots a doctor already has booked between two days.
   *
   * Stays idle until a doctor is chosen: the path carries the id, so without
   * one there is no URL to request.
   *
   * @param doctorId Returns the chosen doctor, or null when none is.
   * @param from Returns the first day as `yyyy-MM-dd`.
   * @param to Returns the last day as `yyyy-MM-dd`.
   * @returns The resource holding that doctor's booked slots.
   */
  getDoctorSchedulesResource = (doctorId: () => number | null, from: () => string, to: () => string) =>
    this.apiService.getResource<GetDoctorSchedulesResponse>(
      () => (doctorId() ? `/Appointments/doctor/${doctorId()}/schedules` : null),
      () => ({ from: from(), to: to() }),
    );
}
