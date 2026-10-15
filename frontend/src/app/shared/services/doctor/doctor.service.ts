import { inject, Service } from '@angular/core';

import { ApiService } from '@core/services/api/api.service';

import { CreateDoctorBody, Doctor, DoctorFilters, UpdateDoctorBody } from './doctor.types';

@Service()
export class DoctorService {
  apiService = inject(ApiService);

  /**
   * Doctors as reactive state, filtered however the caller wants.
   *
   * @param filters Returns the query parameters; defaults to no filter at all.
   * @returns The resource holding the doctor list.
   */
  getDoctorResource = (filters: () => DoctorFilters = () => ({})) => this.apiService.getPagedResource<Doctor>('/Doctors', filters);

  /**
   * One doctor.
   *
   * @param id The doctor.
   */
  getDoctor = (id: number) => this.apiService.get<Doctor>(`/Doctors/${id}`);

  /**
   * Add a doctor. This also creates their person record and their login, so
   * it is the only way a doctor becomes bookable.
   *
   * @param body The person, their credentials and their qualifications.
   */
  createDoctor = (body: CreateDoctorBody) => this.apiService.post<Doctor>('/Doctors', body);

  /**
   * Correct a doctor's details. The national number, the email and the
   * password are fixed at creation and cannot be changed here.
   *
   * @param id The doctor.
   * @param body The replacement details.
   */
  updateDoctor = (id: number, body: UpdateDoctorBody) => this.apiService.put(`/Doctors/${id}`, body);

  /**
   * Remove a doctor outright. The API refuses one whose account is already
   * deactivated, and the row still carries their appointments and visits, so
   * deactivating the account is usually what is wanted instead.
   *
   * @param id The doctor.
   */
  deleteDoctor = (id: number) => this.apiService.delete(`/Doctors/${id}`);
}
