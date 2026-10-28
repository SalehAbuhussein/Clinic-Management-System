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
