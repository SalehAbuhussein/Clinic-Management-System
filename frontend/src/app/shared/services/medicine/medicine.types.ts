export type Medicine = {
  id: number;
  rxCUI: number;
  genericname: string;
  brandname: string;
  form: string;
  strength: string;
};

export type MedicineFilters = {
  name?: string;
  page?: number;
  perPage?: number;
};

export type CreateMedicineRequest = {
  rxCUI: number;
  genericname: string;
  brandname?: string;
  form: string;
  strength: string;
};

export type UpdateMedicineRequest = {
  genericname: string;
  brandname?: string;
  form: string;
  strength: string;
};
