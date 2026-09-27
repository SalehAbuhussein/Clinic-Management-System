import { inject, Service } from '@angular/core';

import { ApiService } from '@core/services/api/api.service';

import { AppointmentFilters, CreateAppointmentRequest, CreateDoctorAppoinmentResponse, GetAppointmentsResponse, GetDoctorSchedulesResponse } from './appointment.types';

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

  /**
   * Create Appointment for a patient
   * 
   * @param body 
   * @returns 
   */
  createAppointment = (body: CreateAppointmentRequest) => this.apiService.post<CreateDoctorAppoinmentResponse>("/Appointments", body);

  /**
   * Record that the patient came. One way: the API has no route back to
   * Scheduled, so a mis-click needs an admin.
   *
   * @param id The appointment.
   */
  attendAppointment = (id: number) => this.apiService.patch(`/Appointments/${id}/attend`);

  /**
   * Record that the patient did not come.
   *
   * @param id The appointment.
   */
  markAppointmentNoShow = (id: number) => this.apiService.patch(`/Appointments/${id}/no-show`);

  /**
   * Cancel the appointment. The row keeps its place in the day with status 3,
   * so the slot still reads as booked and then freed.
   *
   * @param id The appointment.
   */
  cancelAppointment = (id: number) => this.apiService.patch(`/Appointments/${id}/cancel`);
}
