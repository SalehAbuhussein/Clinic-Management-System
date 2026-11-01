import { Medicine } from '@shared/services/medicine/medicine.types';
import { Visit } from '@shared/services/visit/visit.types';

export type VisitDetailAction = 'diagnose' | 'prescribe' | 'edit' | 'checkout' | 'delete';

export type VisitDetailData = {
  visit: Visit;
  readOnly?: boolean;
};

export type VisitEditFormValue = {
  checkinTime: string;
  notes: string;
};

export type PrescriptionLine = {
  key: number;
  medicine: Medicine | null;
  dosage: string;
  frequency: string;
  duration: string;
};
