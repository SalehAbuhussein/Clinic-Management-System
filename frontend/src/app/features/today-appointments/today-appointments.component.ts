import { Component, computed, inject, linkedSignal, signal } from '@angular/core';
import { formatDate } from '@angular/common';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';

import { AppointmentService } from '@shared/services/appointment/appointment.service';

import { AppointmentCardShimmerComponent } from '@shared/components/appointment-card-shimmer/appointment-card-shimmer.component';
import { AppointmentsListComponent } from './components/appointments-list/appointments-list.component';
import { AppointmentsListEmptyStateComponent } from './components/appointments-list-empty-state/appointments-list-empty-state.component';
import { AppointmentsListErrorStateComponent } from './components/appointments-list-error-state/appointments-list-error-state.component';
import { PaginationComponent } from '@shared/components/pagination/pagination.component';

@Component({
  imports: [
    AppointmentCardShimmerComponent,
    AppointmentsListComponent,
    AppointmentsListEmptyStateComponent,
    AppointmentsListErrorStateComponent,
    MatButton,
    MatIcon,
    MatIconButton,
    PaginationComponent,
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

  page = linkedSignal({
    source: this.dayParam,
    computation: () => 1
  });
  total = computed(() => this.appointmentsResource.hasValue() ? this.appointmentsResource.value().total : 0);
  perPage = computed(() => this.appointmentsResource.hasValue() ? this.appointmentsResource.value().perPage : 10);

  appointmentsResource = this.appointmentService.getTodayAppointmentResource(this.dayParam, this.page);
  appointments = computed(() => this.appointmentsResource.hasValue() ? this.appointmentsResource.value().items : []);

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
