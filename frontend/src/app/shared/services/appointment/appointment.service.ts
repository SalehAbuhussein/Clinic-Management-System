import { tap } from 'rxjs';

import { inject, Service, signal } from '@angular/core';

import { ApiService } from '@core/services/api/api.service';

import { dayRange, rangeBetween } from '@core/utils/day.utils';

import { AppointmentCard, AppointmentFilters, AppointmentSchedule, CreateAppointmentRequest, CreateDoctorAppoinmentResponse, RescheduleAppointmentRequest } from './appointment.types';

@Service()
export class AppointmentService {
  apiService = inject(ApiService);

  /**
   * The id of the last appointment booked, which a screen can watch to refetch.
   * The sidebar can book from anywhere, so the list behind it has no other way
   * to hear about it.
   */
  lastBookedId = signal(0);

  /**
   * Appointments as reactive state, filtered however the caller wants.
   *
   * @param filters Returns the query parameters; defaults to no filter at all.
   * @returns The resource holding the appointment list.
   */
  getAppointmentResource = (filters: () => AppointmentFilters = () => ({})) => this.apiService.getPagedResource<AppointmentCard>('/Appointments', filters);

  /**
   * Appointments across a range of days.
   *
   * The bounds go out as instants carrying this browser's offset, because the
   * API compares them against stored instants. A bare date would widen to UTC
   * midnight in SQL Server and cut the day at the wrong hour.
   *
   * @param from Returns the first day to include; the time of day is ignored.
   * @param to Returns the last day to include.
   * @returns The resource holding those appointments.
   */
  getAppointmentsInRangeResource = (from: () => Date, to: () => Date, page: () => number) => this.getAppointmentResource(() => ({
    ...rangeBetween(from(), to()),
    page: page(),
  }));

  /**
   * The slots a doctor already has booked between two days.
   *
   * @param doctorId Returns the chosen doctor, or null when none is.
   * @param day Returns the day being booked, or null while none is chosen.
   * @returns The resource holding that doctor's booked slots.
   */
  getDoctorSchedulesResource = (doctorId: () => number | null, day: () => Date | null) =>
    this.apiService.getPagedResource<AppointmentSchedule>(
      () => (doctorId() && day() ? `/Appointments/doctor/${doctorId()}/schedules` : null),
      () => dayRange(day()!),
    );

  /**
   * Create Appointment for a patient
   *
   * @param body
   * @returns
   */
  createAppointment = (body: CreateAppointmentRequest) =>
    this.apiService.post<CreateDoctorAppoinmentResponse>("/Appointments", body)
      .pipe(tap((booked) => booked && this.lastBookedId.set(booked.id)));

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

  /**
   * Move an appointment. Only the time window and the notes can change; the
   * patient and the doctor are fixed once it exists.
   *
   * @param id The appointment.
   * @param body The new window.
   */
  rescheduleAppointment = (id: number, body: RescheduleAppointmentRequest) => this.apiService.patch(`/Appointments/${id}`, body);
}
