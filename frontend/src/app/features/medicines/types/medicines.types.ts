export type MedicineDetailAction = 'edit';

export type MedicineFormData = {
  rxCUI: number | null;
  genericname: string;
  brandname: string;
  form: string;
  strength: string;
};
