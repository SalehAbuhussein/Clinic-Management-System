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
  dateOfBirth: string;
  phone: string | null;
  emergencyContactName: string | null;
  emergencyContactNumber: string | null;
  personID: number;
};

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

export type PatientFormValue = {
  firstname: string;
  midname: string;
  thirdname: string;
  lastname: string;
  nationalNo: string;
  phone: string;
  gender: boolean | null;
  dateOfBirth: Date | null;
  emergencyContactName: string;
  emergencyContactNumber: string;
};
