import { Role } from '@core/auth/types/auth.types';

export type UserDetailAction = 'edit' | 'activate' | 'deactivate';

export type UserFilterField = 'fullname' | 'email' | 'active';

export type UserFormValue = {
  firstname: string;
  midname: string;
  thirdname: string;
  lastname: string;
  nationalNo: string;
  phone: string;
  gender: boolean | null;
  dateOfBirth: Date | null;
  email: string;
  password: string;
  role: Role;
};
