import { PagedResult } from '@core/types/api.types';

export type DoctorFilters = {
  fullname?: string;
  specialization?: string;
  active?: string;
  page?: number;
  perPage?: number;
};

export type Doctor = {
  id: number;
  fullname: string | null;
  firstname: string;
  midname: string;
  thirdname: string | null;
  lastname: string;
  nationalNo: string;
  phone: string | null;
  gender: boolean;
  dateOfBirth: string | null;
  specialization: string;
  medicalSchool: string;
  graduationYear: number;
  personID: number;
  active: boolean;
};
export type CreateDoctorBody = {
  firstname: string;
  midname: string;
  thirdname?: string;
  lastname: string;
  nationalNo: string;
  phone: string;
  gender: boolean;
  dateOfBirth: string;
  specialization: string;
  medicalSchool: string;
  graduationYear: number;
  email: string;
  password: string;
};

export type UpdateDoctorBody = {
  firstname: string;
  midname: string;
  thirdname?: string;
  lastname: string;
  phone: string;
  gender: boolean;
  dateOfBirth: string;
  specialization: string;
  medicalSchool: string;
  graduationYear: number;
};

export type GetDoctorsResponse = PagedResult<Doctor>;
