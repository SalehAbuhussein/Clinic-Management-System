import { ActivatedRoute, Router } from '@angular/router';
import { Component, computed, effect, inject, linkedSignal, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialog } from '@angular/material/dialog';
import { MatIcon } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { formatDate } from '@angular/common';

import { Observable } from 'rxjs';

import { AppointmentService } from '@shared/services/appointment/appointment.service';
import { VisitService } from '@shared/services/visit/visit.service';

import { AppointmentCardShimmerComponent } from '@shared/components/appointment-card-shimmer/appointment-card-shimmer.component';
import { AppointmentsListComponent } from './components/appointments-list/appointments-list.component';
import { AppointmentsListEmptyStateComponent } from './components/appointments-list-empty-state/appointments-list-empty-state.component';
import { AppointmentsListErrorStateComponent } from './components/appointments-list-error-state/appointments-list-error-state.component';
import { AppointmentDetailAction, AppointmentDetailDialogComponent } from './components/appointment-detail-dialog/appointment-detail-dialog.component';
import { BookAppointmentDialogComponent } from '@features/book-appointment-dialog/book-appointment-dialog.component';
import { ConfirmDialogComponent } from '@shared/components/confirm-dialog/confirm-dialog.component';
import { ConfirmDialogData } from '@shared/components/confirm-dialog/confirm-dialog.types';
import { PaginationComponent } from '@shared/components/pagination/pagination.component';

import { ApiError } from '@core/auth/types/auth.types';
import { AppointmentCard } from '@shared/services/appointment/appointment.types';
import { BookedAppointment } from '@features/book-appointment-dialog/types/book-appointment.types';

@Component({
  imports: [
    AppointmentCardShimmerComponent,
    AppointmentsListComponent,
    AppointmentsListEmptyStateComponent,
    AppointmentsListErrorStateComponent,
    FormsModule,
    MatButton,
    MatDatepickerModule,
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
  route = inject(ActivatedRoute);
  router = inject(Router);
  snackbar = inject(MatSnackBar);
  visitService = inject(VisitService);

  day = signal(this.dayFromUrl());

  dayParam = computed(() => formatDate(this.day(), 'yyyy-MM-dd', 'en-US'));
  dayLabel = computed(() => formatDate(this.day(), 'EEEE d MMMM', 'en-US'));
  isToday = computed(() => this.dayParam() === formatDate(new Date(), 'yyyy-MM-dd', 'en-US'));

  page = linkedSignal<string, number>({
    source: this.dayParam,
    computation: (_, previous) => previous ? 1 : this.pageFromUrl(),
  });
  total = computed(() => this.appointmentsResource.hasValue() ? this.appointmentsResource.value().total : 0);
  perPage = computed(() => this.appointmentsResource.hasValue() ? this.appointmentsResource.value().perPage : 10);

  appointmentsResource = this.appointmentService.getTodayAppointmentResource(this.dayParam, this.page);
  appointments = computed(() => this.appointmentsResource.hasValue() ? this.appointmentsResource.value().items : []);

  constructor() {
    effect(() => {
      this.router.navigate([], {
        queryParams: { day: this.dayParam(), page: this.page() },
        relativeTo: this.route,
        replaceUrl: true,
      });
    });
  }

  /**
   * Reads the day to show. A date with no time parses as UTC, so the midnight
   * is spelled out to keep it local.
   *
   * @returns The day from the URL, or today.
   */
  dayFromUrl(): Date {
    const value = this.route.snapshot.queryParamMap.get('day');
    const day = value ? new Date(`${value}T00:00:00`) : null;

    return day && !Number.isNaN(day.getTime()) ? day : new Date();
  }

  /**
   * @returns The page from the URL, or the first one.
   */
  pageFromUrl(): number {
    const page = Number(this.route.snapshot.queryParamMap.get('page'));

    return Number.isInteger(page) && page > 0 ? page : 1;
  }

  /**
   * Move the shown day.
   *
   * @param days Offset in days; negative goes back.
   */
  shiftDayBy = (days: number) => this.day.update((current) => new Date(current.getFullYear(), current.getMonth(), current.getDate() + days));

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

  /**
   * Show everything on an appointment, and act on whatever the dialog closes
   * with.
   *
   * Each action opens another dialog, so the detail closes first rather than
   * stacking two on top of each other.
   *
   * @param appointment The card that was picked.
   */
  openAppointment = (appointment: AppointmentCard) => {
    this.dialog.open(AppointmentDetailDialogComponent, { width: '480px', data: appointment })
      .afterClosed()
      .subscribe((action: AppointmentDetailAction | undefined) => {
        switch (action) {
          case 'checkIn':
            this.checkInPatient(appointment.id);
            break;
          case 'reschedule':
            this.rescheduleAppointment(appointment);
            break;
          case 'noShow':
            this.markNoShow(appointment.id);
            break;
          case 'cancel':
            this.cancelAppointment(appointment.id);
            break;
        }
      });
  }

  /**
   * Move an appointment. The booking dialog opens with the people fixed and
   * the window pre-filled, because only the time and the notes can change.
   *
   * @param appointment The row that was picked.
   */
  rescheduleAppointment = (appointment: AppointmentCard) => {
    this.dialog.open(BookAppointmentDialogComponent, { width: '560px', data: appointment })
      .afterClosed()
      .subscribe((moved: BookedAppointment | undefined) => {
        if (!moved) {
          return;
        }

        this.appointmentsResource.reload();
      });
  }

  /**
   * Open a visit for a patient who has arrived. This also marks the
   * appointment attended, so it replaces that step rather than following it.
   *
   * @param id The appointment.
   */
  checkInPatient = (id: number) => this.confirmThen(
    {
      title: 'Check this patient in?',
      message: 'This opens their visit and marks the appointment attended. There is no way back to Scheduled.',
      confirmLabel: 'Check in',
    },
    this.visitService.checkIn(id),
    'Patient checked in',
  );

  /**
   * Record that a patient did not come.
   *
   * @param id The appointment.
   */
  markNoShow = (id: number) => this.confirmThen(
    {
      title: 'Mark this patient as a no-show?',
      message: 'This records that the patient did not come. There is no way back to Scheduled.',
      confirmLabel: 'Mark no-show',
    },
    this.appointmentService.markAppointmentNoShow(id),
    'Marked as no-show',
  );

  /**
   * Cancel an appointment. The row keeps its place in the day.
   *
   * @param id The appointment.
   */
  cancelAppointment = (id: number) => this.confirmThen(
    {
      title: 'Cancel this appointment?',
      message: 'It stays in the day marked cancelled, so the slot reads as booked and then freed. This cannot be undone.',
      confirmLabel: 'Cancel appointment',
      dismissLabel: 'Keep appointment',
      destructive: true,
    },
    this.appointmentService.cancelAppointment(id),
    'Appointment cancelled',
  );

  /**
   * Asks before a status change, then runs it.
   *
   * Every one of them is one way: the API has no route back to Scheduled, so a
   * mis-click on any of them needs an admin to fix.
   *
   * @param data What the dialog should say.
   * @param request The call to make once confirmed. Cold, so nothing is sent
   * until the subscribe inside runStatusChange. Its value is ignored; only
   * success or failure matters here.
   * @param message What to say when it succeeds.
   */
  confirmThen(data: ConfirmDialogData, request: Observable<unknown>, message: string): void {
    this.dialog.open(ConfirmDialogComponent, { width: '400px', data })
      .afterClosed()
      .subscribe((confirmed: boolean | undefined) => {
        if (!confirmed) {
          return;
        }

        this.runStatusChange(request, message);
      });
  }

  /**
   * Runs one of the status transitions and refreshes the day.
   *
   * The endpoints differ only in what they send, so the confirmation, the
   * refresh and the error handling are written once.
   *
   * @param request The call to make.
   * @param message What to say when it succeeds.
   */
  runStatusChange(request: Observable<unknown>, message: string): void {
    request.subscribe({
      next: () => {
        this.snackbar.open(message, 'Dismiss', { duration: 4000 });
        this.appointmentsResource.reload();
      },
      error: (error: ApiError) => this.snackbar.open(error.message, 'Dismiss', { duration: 8000 }),
    });
  }
}
