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

export type GetAppointmentsResponse = AppointmentCard[];
