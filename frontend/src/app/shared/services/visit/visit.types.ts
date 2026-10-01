export type Visit = {
  id: number;
  appointmentID: number;
  checkinTime: string;
  checkoutTime: string | null;
  notes: string | null;
};
