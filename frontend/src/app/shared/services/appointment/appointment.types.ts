import { PagedResult } from '@core/types/api.types';

export type AppointmentFilters = {
  doctorId?: number;
  patientId?: number;
  from?: string;
  to?: string;
  page?: number;
  perPage?: number;
};

export type AppointmentCard = {
  id: number;
  doctorId: number;
  doctorName: string;
  patientId: number;
  patientName: string;
  startAt: string;
  endAt: string;
  status: number;
  notes: string | null;
};

export type GetAppointmentsResponse = PagedResult<AppointmentCard>;

export type AppointmentSchedule = {
  appointmentID: number;
  startAt: string;
  endAt: string;
  status: number;
};

export type GetDoctorSchedulesResponse = AppointmentSchedule[];
