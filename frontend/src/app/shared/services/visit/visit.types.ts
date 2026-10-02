export type Visit = {
  id: number;
  appointmentID: number;
  patientName: string | null;
  doctorName: string | null;
  appointmentStartAt: string | null;
  checkinTime: string;
  checkoutTime: string | null;
  notes: string | null;
};
