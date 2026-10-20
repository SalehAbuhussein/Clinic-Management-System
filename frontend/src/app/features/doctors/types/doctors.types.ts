export type DoctorDetailAction = 'edit';

export type DoctorFilterField = 'fullname' | 'specialization' | 'active';

export type DoctorFormData = {
  firstname: string;
  midname: string;
  thirdname: string;
  lastname: string;
  nationalNo: string;
  phone: string;
  gender: boolean | null;
  dateOfBirth: Date | null;
  specialization: string;
  medicalSchool: string;
  graduationYear: number | null;
  email: string;
  password: string;
};
