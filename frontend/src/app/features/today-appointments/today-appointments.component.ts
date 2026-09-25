import { Component, computed, inject, linkedSignal, signal } from '@angular/core';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIcon } from '@angular/material/icon';
import { formatDate } from '@angular/common';


import { AppointmentService } from '@shared/services/appointment/appointment.service';

import { AppointmentCardShimmerComponent } from '@shared/components/appointment-card-shimmer/appointment-card-shimmer.component';
import { AppointmentsListComponent } from './components/appointments-list/appointments-list.component';
import { AppointmentsListEmptyStateComponent } from './components/appointments-list-empty-state/appointments-list-empty-state.component';
import { AppointmentsListErrorStateComponent } from './components/appointments-list-error-state/appointments-list-error-state.component';
import { BookAppointmentDialogComponent } from '@features/book-appointment-dialog/book-appointment-dialog.component';
import { PaginationComponent } from '@shared/components/pagination/pagination.component';
import { BookedAppointment } from '@features/book-appointment-dialog/types/book-appointment.types';


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
  dialog = inject(MatDialog);

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

  /**
   * Open the booking dialog and show the new appointment.
   *
   * The dialog confirms the booking itself; this screen owns the one thing the
   * dialog cannot do, which is refresh the day behind it.
   */
  openBookingDialog = () => {
    this.dialog.open(BookAppointmentDialogComponent, { width: '560px' })
    .afterClosed()
    .subscribe((booked: BookedAppointment | undefined) => {
      if (!booked) {
        return;
      }

      this.appointmentsResource.reload();
    });
  }
}
