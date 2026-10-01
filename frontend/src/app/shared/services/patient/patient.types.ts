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
  firstname: string | null;
  midname: string | null;
  thirdname: string | null;
  lastname: string | null;
  nationalNo: string | null;
  gender: boolean | null;
  dateOfBirth: string | null;
  phone: string | null;
  emergencyContactName: string | null;
  emergencyContactNumber: string | null;
  personID: number;
};

export type GetPatientsResponse = PagedResult<Patient>;

export type CreatePatientRequest = {
  firstname: string;
  midname: string;
  thirdname?: string;
  lastname: string;
  nationalNo: string;
  phone: string;
  gender: boolean;
  dateOfBirth: string;
  emergencyContactName?: string;
  emergencyContactNumber?: string;
};

export type UpdatePatientRequest = Omit<CreatePatientRequest, 'nationalNo'>;
