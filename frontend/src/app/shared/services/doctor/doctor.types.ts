import { PagedResult } from '@core/types/api.types';

export type DoctorFilters = {
  fullname?: string;
  specialization?: string;
  page?: number;
  perPage?: number;
};

export type Doctor = {
  id: number;
  fullname: string | null;
  phone: string | null;
  specialization: string;
  medicalSchool: string;
  graduationYear: number;
  personID: number;
  active: boolean;
};

export type GetDoctorsResponse = PagedResult<Doctor>;
