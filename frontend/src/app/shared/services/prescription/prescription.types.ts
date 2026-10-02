export type PrescriptionItem = {
  id: number;
  visitID: number;
  medicineID: number;
  dosage: string;
  frequency: string;
  duration: string;
  genericname: string;
  brandname: string | null;
  form: string;
  strength: string;
};

export type PrescriptionRequestItem = {
  medicineId: number;
  dosage: string;
  frequency: string;
  duration: string;
};

export type PrescriptionRequest = {
  items: PrescriptionRequestItem[];
};
