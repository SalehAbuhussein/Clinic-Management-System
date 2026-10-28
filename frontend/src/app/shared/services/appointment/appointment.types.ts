export const AppointmentStatus = {
  Scheduled: 0,
  Attended: 1,
  NoShow: 2,
  Cancelled: 3,
} as const;

export type AppointmentStatus = typeof AppointmentStatus[keyof typeof AppointmentStatus];

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

export type RescheduleAppointmentRequest = {
  startAt: string;
  endAt: string;
  notes?: string;
};
