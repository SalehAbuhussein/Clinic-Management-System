/**
 * The form behind the dialog. One shape for both modes: editing simply leaves
 * the national number, email and password untouched, because the API fixes
 * those at creation.
 */
export type DoctorDetailAction = 'edit';

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
