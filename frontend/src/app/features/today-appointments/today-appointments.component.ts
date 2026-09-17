import { Component, computed, inject, signal } from '@angular/core';
import { formatDate } from '@angular/common';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';

import { AppointmentService } from '@shared/services/appointment/appointment.service';

import { AppointmentCardShimmerComponent } from '@shared/components/appointment-card-shimmer/appointment-card-shimmer.component';
import { AppointmentsListComponent } from './components/appointments-list/appointments-list.component';
import { AppointmentsListEmptyStateComponent } from './components/appointments-list-empty-state/appointments-list-empty-state.component';
import { AppointmentsListErrorStateComponent } from './components/appointments-list-error-state/appointments-list-error-state.component';

@Component({
  imports: [
    AppointmentsListComponent,
    AppointmentCardShimmerComponent,
    AppointmentsListErrorStateComponent,
    AppointmentsListEmptyStateComponent,
    MatButton,
    MatIconButton,
    MatIcon,
  ],
  selector: 'app-today-appointments',
  styleUrl: './today-appointments.component.scss',
  templateUrl: './today-appointments.component.html',
})
export class TodayAppointmentsComponent {
  appointmentService = inject(AppointmentService);

  day = signal(new Date());

  dayParam = computed(() => formatDate(this.day(), 'yyyy-MM-dd', 'en-US'));
  dayLabel = computed(() => formatDate(this.day(), 'EEEE d MMMM', 'en-US'));
  isToday = computed(() => this.dayParam() === formatDate(new Date(), 'yyyy-MM-dd', 'en-US'));

  appointmentsResource = this.appointmentService.getTodayAppointmentResource(this.dayParam);

  /**
   * Move the shown day.
   *
   * @param days Offset in days; negative goes back.
   */
  shiftDay = (days: number) => this.day.update((current) => new Date(current.getFullYear(), current.getMonth(), current.getDate() + days));

  /**
   * Jump back to the current day.
   */
  goToToday = () => this.day.set(new Date());
}
