import { inject, Service } from '@angular/core';

import { ApiService } from '@core/services/api/api.service';

import { GetAppointmentsResponse } from './appointment.types';

@Service()
export class AppointmentService {
  apiService = inject(ApiService);

  /**
   * Get Appointment List
   */
  getAppointmentList = () => this.apiService.get<GetAppointmentsResponse>('/Appointments');

  /**
   * Get Appointment List Resource
   */
  getAppointmentResource = () => this.apiService.getResource<GetAppointmentsResponse>('/Appointments');
}
