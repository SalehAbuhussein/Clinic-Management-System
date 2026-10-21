import { ActivatedRoute, Router } from '@angular/router';
import { ChangeDetectionStrategy, Component, computed, effect, inject, linkedSignal, signal } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { formatDate } from '@angular/common';

import { Observable } from 'rxjs';

import { AppointmentService } from '@shared/services/appointment/appointment.service';
import { VisitService } from '@shared/services/visit/visit.service';

import { AppointmentCardShimmerComponent } from '@shared/components/appointment-card-shimmer/appointment-card-shimmer.component';
import { AppointmentDetailDialogComponent } from './components/appointment-detail-dialog/appointment-detail-dialog.component';
import { AppointmentsListComponent } from './components/appointments-list/appointments-list.component';
import { AppointmentsListEmptyStateComponent } from './components/appointments-list-empty-state/appointments-list-empty-state.component';
import { AppointmentsListErrorStateComponent } from './components/appointments-list-error-state/appointments-list-error-state.component';
import { BookAppointmentDialogComponent } from '@features/book-appointment/components/book-appointment-dialog/book-appointment-dialog.component';
import { ConfirmDialogComponent } from '@shared/components/confirm-dialog/confirm-dialog.component';
import { ConfirmDialogData } from '@shared/components/confirm-dialog/confirm-dialog.types';
import { DateRangePickerComponent } from '@shared/components/date-range-picker/date-range-picker.component';
import { ExportButtonComponent } from '@shared/components/export-button/export-button.component';
import { PaginationComponent } from '@shared/components/pagination/pagination.component';

import { ApiError } from '@core/auth/types/auth.types';
import { AppointmentCard } from '@shared/services/appointment/appointment.types';
import { AppointmentDetailAction } from './types/appointments.types';
import { BookedAppointment } from '@features/book-appointment/types/book-appointment.types';

import { pageFromUrl } from '@core/utils/url-state.utils';
import { rangeBetween } from '@core/utils/day.utils';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    AppointmentCardShimmerComponent,
    AppointmentsListComponent,
    AppointmentsListEmptyStateComponent,
    AppointmentsListErrorStateComponent,
    DateRangePickerComponent,
    ExportButtonComponent,
    PaginationComponent,
  ],
  selector: 'app-appointments',
  styleUrl: './appointments.component.scss',
  templateUrl: './appointments.component.html',
})
export class AppointmentsComponent {
  appointmentService = inject(AppointmentService);
  dialog = inject(MatDialog);
  route = inject(ActivatedRoute);
  router = inject(Router);
  snackbar = inject(MatSnackBar);
  visitService = inject(VisitService);

  from = signal(this.dateFromUrl('from'));
  to = signal(this.dateFromUrl('to'));

  rangeKey = computed(() => `${this.asParam(this.from())}_${this.asParam(this.to())}`);
  isOneDay = computed(() => this.asParam(this.from()) === this.asParam(this.to()));
  rangeLabel = computed(() => this.isOneDay()
    ? formatDate(this.from(), 'EEEE d MMMM', 'en-US')
    : `${formatDate(this.from(), 'd MMM', 'en-US')} to ${formatDate(this.to(), 'd MMM', 'en-US')}`);

  page = linkedSignal<string, number>({
    source: this.rangeKey,
    computation: (_, previous) => previous ? 1 : pageFromUrl(this.route),
  });
  total = computed(() => this.appointmentsResource.value()?.total);
  perPage = computed(() => this.appointmentsResource.value()?.perPage || 10);

  /**
   * What the export covers: the day on screen, and nothing about paging. It
   * reuses the same dayRange the list resource does, so the file and the screen
   * can never disagree about where the day starts.
   */
  exportFilters = computed(() => rangeBetween(this.from(), this.to()));

  appointmentsResource = this.appointmentService.getAppointmentsInRangeResource(this.from, this.to, this.page);
  appointments = computed(() => this.appointmentsResource.value()?.items);

  constructor() {
    // A booking made from the sidebar never reaches this screen's own dialog
    // handler, so the service's last-booked id is what tells it to refetch.
    effect(() => {
      if (this.appointmentService.lastBookedId()) {
        this.appointmentsResource.reload();
      }
    });

    effect(() => {
      this.router.navigate([], {
        queryParams: { from: this.asParam(this.from()), to: this.asParam(this.to()), page: this.page() },
        relativeTo: this.route,
        replaceUrl: true,
      });
    });
  }


  /**
   * Reads one end of the range. A date with no time parses as UTC, so the
   * midnight is spelled out to keep it local.
   *
   * @param key Which query parameter to read.
   * @returns That day, or today when the URL does not say.
   */
  dateFromUrl(key: 'from' | 'to'): Date {
    const value = this.route.snapshot.queryParamMap.get(key);
    const day = value ? new Date(`${value}T00:00:00`) : null;

    return day && !Number.isNaN(day.getTime()) ? day : new Date();
  }

  asParam = (day: Date) => formatDate(day, 'yyyy-MM-dd', 'en-US');

  /**
   * Open the booking dialog. The constructor's effect refreshes the list once
   * a booking lands, so there is nothing to do when it closes.
   */
  openBookingDialog = () => {
    this.dialog.open(BookAppointmentDialogComponent, { width: '560px' });
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
          case 'openVisit':
            this.openVisit(appointment);
            break;
        }
      });
  }

  /**
   * Follow a checked-in appointment to its visit. The visits screen owns every
   * action a visit has, so this hands over rather than repeating them here.
   *
   * @param appointment The row that was picked; its visitId is set.
   */
  openVisit = (appointment: AppointmentCard) => this.router.navigate(
    ['/dashboard/visits'],
    { queryParams: { visitId: appointment.visitId } },
  );

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
