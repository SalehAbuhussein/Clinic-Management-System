import { Role } from '@core/auth/types/auth.types';

export type Profile = {
  userId: number;
  personId: number;
  email: string;
  role: Role;
  firstname: string;
  midname: string;
  thirdname: string | null;
  lastname: string;
  nationalNo: string;
  phone: string;
  gender: boolean;
  dateOfBirth: string;
  profileImage: string | null;
};

export type UpdateProfileRequest = {
  firstname: string;
  midname: string;
  thirdname?: string;
  lastname: string;
  phone: string;
  gender: boolean;
  dateOfBirth: string;
  profileImage?: string;
};
