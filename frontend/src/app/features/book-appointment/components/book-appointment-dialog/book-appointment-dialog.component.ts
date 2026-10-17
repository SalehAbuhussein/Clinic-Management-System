import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { formatDate } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogContent, MatDialogTitle, MatDialogActions, MatDialogClose, MatDialogRef } from '@angular/material/dialog';
import { MatButton } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormField, MatLabel, MatHint } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatInput } from '@angular/material/input';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';

import { AppointmentService } from '@shared/services/appointment/appointment.service';

import { DateInputComponent } from '../date-input/date-input.component';
import { DoctorDayStripComponent } from '../doctor-day-strip/doctor-day-strip.component';
import { DoctorSelectorInputComponent } from '../doctor-selector-input/doctor-selector-input.component';
import { PatientSelectorInputComponent } from '../patient-selector-input/patient-selector-input.component';

import { ApiError } from '@core/auth/types/auth.types';
import { AppointmentCard, CreateAppointmentRequest, RescheduleAppointmentRequest } from '@shared/services/appointment/appointment.types';
import { Doctor } from '@shared/services/doctor/doctor.types';
import { Patient } from '@shared/services/patient/patient.types';

@Component({
  imports: [
    DateInputComponent,
    DoctorDayStripComponent,
    DoctorSelectorInputComponent,
    FormsModule,
    MatButton,
    MatChipsModule,
    MatDialogActions,
    MatDialogClose,
    MatDialogContent,
    MatDialogTitle,
    MatFormField,
    MatHint,
    MatIcon,
    MatInput,
    MatLabel,
    MatProgressSpinner,
    PatientSelectorInputComponent,
  ],
  selector: 'app-book-appointment-dialog',
  styleUrl: './book-appointment-dialog.component.scss',
  templateUrl: './book-appointment-dialog.component.html',
})
export class BookAppointmentDialogComponent implements OnInit {
  appointmentService = inject(AppointmentService);
  dialogRef = inject(MatDialogRef<BookAppointmentDialogComponent>);
  snackbar = inject(MatSnackBar);
  existingAppointment = inject<AppointmentCard | null>(MAT_DIALOG_DATA, { optional: true });

  readonly DURATIONS = [15, 30, 45, 60];
  readonly isRescheduling = !!this.existingAppointment;

  patient = signal<Patient | null>(null);
  doctor = signal<Doctor | null>(null);
  date = signal<Date | null>(new Date());
  time = signal("09:30");
  minutes = signal(30);
  notes = signal('');
  isSaving = signal(false);
  error = signal('');

  canBook = computed(() => this.isRescheduling
    ? !!this.date() && !!this.time()
    : !!this.patient() && !!this.doctor() && !!this.date() && !!this.time());

  /**
   * Angular OnInit lifecycle method
   */
  ngOnInit(): void {
    this.initComponent();
  }

  /**
   * Everything the dialog needs before its first render.
   */
  initComponent = (): void => {
    this.attemptFillingResheduleData();
  }

  /**
   * If Dialog Data is present this will proceed to fill the form with existing Booking data
   */
  attemptFillingResheduleData = (): void => {
    const existing = this.existingAppointment;

    if (!existing) {
      return;
    }

    const startAt = new Date(existing.startAt);
    const endAt = new Date(existing.endAt);

    this.date.set(startAt);
    this.time.set(this.toTimeText(startAt));
    this.minutes.set(Math.round((endAt.getTime() - startAt.getTime()) / 60_000));
    this.notes.set(existing.notes ?? '');
  }

  /**
   * Submits the form. Which of the two calls it makes is the only thing the
   * dialog's mode decides; everything above this point is shared.
   */
  bookAppointment = () => {
    if (!this.canBook() || this.isSaving()) {
      return;
    }

    this.isSaving.set(true);
    this.error.set('');

    if (this.existingAppointment) {
      this.rescheduleAppointment(this.existingAppointment, this.getTimeWindow());
      return;
    }

    this.createAppointment(this.getAppointmentBookingRequest());
  }

  /**
   * Books a new appointment. The id comes back on the response, because until
   * this call there was nothing to have one.
   * @param body New Appointment Data
   */
  createAppointment = (body: CreateAppointmentRequest): void => {
    this.appointmentService.createAppointment(body).subscribe({
      next: (response) => this.finish(response!.id, this.patient()!.fullname ?? '', body.startAt, 'booked'),
      error: this.fail,
    });
  }

  /**
  * Moves an existing appointment. The API answers 204, so the id and the
  * patient come from the row that was opened rather than from the response.
  *
  * @param existing The appointment being moved.
  * @param body The new window. Only its times and notes are sent.
  */
  rescheduleAppointment = (existing: AppointmentCard, body: RescheduleAppointmentRequest): void => {
    this.appointmentService
    .rescheduleAppointment(existing.id, body)
    .subscribe({
        next: () => this.finish(existing.id, existing.patientName, body.startAt, 'moved'),
        error: this.fail,
      });
  }

  /**
   * The window and the notes, which both endpoints take.
   *
   * Times are read as local wall clock and sent with the local offset, so the
   * datetimeoffset column keeps what the clock read here. `toISOString` would
   * send the same instant as `...Z` and store +00:00 instead.
   */
  getTimeWindow = (): RescheduleAppointmentRequest => {
    const [hours, minutes] = this.time().split(':').map(Number);
    const startAt = new Date(this.date()!);

    startAt.setHours(hours, minutes, 0, 0);

    const endAt = new Date(startAt.getTime() + this.minutes() * 60_000);

    return {
      startAt: formatDate(startAt, "yyyy-MM-dd'T'HH:mm:ssZZZZZ", 'en-US'),
      endAt: formatDate(endAt, "yyyy-MM-dd'T'HH:mm:ssZZZZZ", 'en-US'),
      notes: this.notes() || undefined,
    };
  }

  /**
   * Build Appointment Booking Request Body
   */
  getAppointmentBookingRequest = (): CreateAppointmentRequest => ({
    patientId: this.patient()!.id,
    doctorId: this.doctor()!.id,
    ...this.getTimeWindow(),
  })

  /**
   * Confirms, then closes with what the caller needs to refresh its list.
   *
   * @param id The appointment that was written.
   * @param patientName Who it is for.
   * @param startAt When it now starts.
   * @param verb What happened to it, for the confirmation.
  */
  finish = (id: number, patientName: string, startAt: string, verb: 'booked' | 'moved'): void => {
    this.snackbar.open(`${patientName} ${verb}`, 'Dismiss', { duration: 5000 });
    this.dialogRef.close({ id, patientName, startAt });
  }

  /**
   * Leaves the form exactly as it was and shows the server's own wording.
  *
  * @param error The mapped failure from the error interceptor.
  */
  fail = (error: ApiError): void => {
    this.isSaving.set(false);
    this.error.set(error.message);
  }

  /**
   * @param date Any moment.
   * @returns Its clock time as `HH:mm`, which is the format the time input reads.
   */
  toTimeText = (date: Date): string => `${String(date.getHours()).padStart(2, '0')}:${String(date.getMinutes()).padStart(2, '0')}`
}
