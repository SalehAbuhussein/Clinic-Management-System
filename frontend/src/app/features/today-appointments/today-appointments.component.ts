import { Component, inject, signal } from '@angular/core';
import { formatDate } from '@angular/common';

import { AppointmentService } from '@shared/services/appointment/appointment.service';

import { AppointmentCardShimmerComponent } from '@shared/components/appointment-card-shimmer/appointment-card-shimmer.component';
import { AppointmentsListComponent } from './components/appointments-list/appointments-list.component';
import { AppointmentsListEmptyStateComponent } from './components/appointments-list-empty-state/appointments-list-empty-state.component';
import { AppointmentsListErrorStateComponent } from './components/appointments-list-error-state/appointments-list-error-state.component';

@Component({
  imports: [AppointmentsListComponent, AppointmentCardShimmerComponent, AppointmentsListErrorStateComponent, AppointmentsListEmptyStateComponent],
  selector: 'app-today-appointments',
  styleUrl: './today-appointments.component.scss',
  templateUrl: './today-appointments.component.html',
})
export class TodayAppointmentsComponent {
  appointmentService = inject(AppointmentService);

  day = signal(new Date());

  appointmentsResource = this.appointmentService.getTodayAppointmentResource(() => formatDate(this.day(), 'yyyy-MM-dd', 'en-US'));
}
