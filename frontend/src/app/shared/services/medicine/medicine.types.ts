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
};
