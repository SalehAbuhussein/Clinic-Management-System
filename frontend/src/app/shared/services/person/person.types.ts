export type Person = {
  id: number;
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

export type PersonFilters = {
  fullname?: string;
  nationalNo?: string;
  phone?: string;
  page?: number;
  perPage?: number;
};

export type UpdatePersonRequest = {
  firstname: string;
  midname: string;
  thirdname?: string;
  lastname: string;
  phone: string;
  gender: boolean;
  dateOfBirth: string;
};
