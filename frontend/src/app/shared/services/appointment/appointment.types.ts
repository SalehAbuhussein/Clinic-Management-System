/** 0 Scheduled, 1 Attended, 2 No-show, 3 Cancelled, as the API stores them. */
export type AppointmentStatus = 0 | 1 | 2 | 3;

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
  visitId: number | null;
  startAt: string;
  endAt: string;
  status: AppointmentStatus;
  notes: string | null;
};

export type AppointmentSchedule = {
  appointmentID: number;
  doctorID: number;
  patientID: number;
  startAt: string;
  endAt: string;
  status: AppointmentStatus;
};

export type CreateAppointmentRequest = {
  doctorId: number;
  patientId: number;
  startAt: string;
  endAt: string;
  notes?: string;
};

export type CreateDoctorAppoinmentResponse = AppointmentCard;

export type RescheduleAppointmentRequest = {
  startAt: string;
  endAt: string;
  notes?: string;
};
