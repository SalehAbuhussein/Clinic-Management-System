import { PagedResult } from '@core/types/api.types';

export type PatientFilters = {
  fullname?: string;
  phone?: string;
  page?: number;
  perPage?: number;
};

export type Patient = {
  id: number;
  fullname: string | null;
  phone: string | null;
  emergencyContactName: string | null;
  emergencyContactNumber: string | null;
  personID: number;
};

export type GetPatientsResponse = PagedResult<Patient>;
