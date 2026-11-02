import { Role } from '@core/auth/types/auth.types';

export type User = {
  id: number;
  fullname: string;
  email: string;
  role: Role;
  active: boolean;
  profileImage: string | null;
};

export type CreateUserRequest = {
  firstname: string;
  midname: string;
  thirdname?: string;
  lastname: string;
  nationalNo: string;
  phone: string;
  gender: boolean;
  dateOfBirth: string;
  email: string;
  password: string;
};

export type UpdateUserRequest = {
  email: string;
  role?: Role;
};

export type UserFilters = {
  fullname?: string;
  email?: string;
  active?: 'true' | 'false';
  page?: number;
  perPage?: number;
};
