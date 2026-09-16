/**
 * What a row on the day needs to render itself.
 *
 * `doctorName` and `patientName` are not on `AppointmentDTO` yet. The API
 * returns ids only, so resolving them client-side would cost one call per row.
 * The backend join is the planned fix; until then the mock supplies them.
 */
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
