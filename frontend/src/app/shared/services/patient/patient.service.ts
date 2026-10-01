import { inject, Service } from '@angular/core';

import { ApiService } from '@core/services/api/api.service';

import { CreatePatientRequest, Patient, PatientFilters, UpdatePatientRequest } from './patient.types';

@Service()
export class PatientService {
  apiService = inject(ApiService);

  /**
   * Patients as reactive state, filtered however the caller wants.
   *
   * @param filters Returns the query parameters; defaults to no filter at all.
   * @returns The resource holding the patient list.
   */
  getPatientResource = (filters: () => PatientFilters = () => ({})) => this.apiService.getPagedResource<Patient>('/Patients', filters);

  /**
   * One patient, with the person fields the list does not carry.
   *
   * @param id The patient.
   */
  getPatient = (id: number) => this.apiService.get<Patient>(`/Patients/${id}`);

  /**
   * Registers a patient and the person behind them.
   *
   * @param body The new patient. A duplicate national number answers 409.
   */
  createPatient = (body: CreatePatientRequest) => this.apiService.post<Patient>('/Patients', body);

  /**
   * Edits a patient. The national number is not in the body: it identifies the
   * record and the API will not change it.
   *
   * @param id The patient.
   * @param body The new details.
   */
  updatePatient = (id: number, body: UpdatePatientRequest) => this.apiService.put(`/Patients/${id}`, body);

  /**
   * Removes a patient. Admin only, and it fails for anyone with appointments.
   *
   * @param id The patient.
   */
  deletePatient = (id: number) => this.apiService.delete(`/Patients/${id}`);
}
