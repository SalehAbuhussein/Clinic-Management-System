import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButton } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialogContent, MatDialogTitle, MatDialogActions, MatDialogClose, MatDialogRef } from '@angular/material/dialog';
import { MatFormField, MatLabel, MatHint } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatInput } from '@angular/material/input';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';

import { AppointmentService } from '@shared/services/appointment/appointment.service';

import { Doctor } from '@shared/services/doctor/doctor.types';
import { Patient } from '@shared/services/patient/patient.types';

import { DateInputComponent } from './components/date-input/date-input.component';
import { DoctorDayStripComponent } from './components/doctor-day-strip/doctor-day-strip.component';
import { DoctorSelectorInputComponent } from './components/doctor-selector-input/doctor-selector-input.component';
import { PatientSelectorInputComponent } from './components/patient-selector-input/patient-selector-input.component';

import { ApiError } from '@core/auth/types/auth.types';
import { CreateAppointmentRequest } from '@shared/services/appointment/appointment.types';

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
export class BookAppointmentDialogComponent {
  appointmentService = inject(AppointmentService);
  dialogRef = inject(MatDialogRef<BookAppointmentDialogComponent>);
  snackbar = inject(MatSnackBar);

  readonly DURATIONS = [15, 30, 45, 60];

  patient = signal<Patient | null>(null);
  doctor = signal<Doctor | null>(null);
  date = signal<Date | null>(new Date());
  time = signal("09:30");
  minutes = signal(30);
  notes = signal('');
  isSaving = signal(false);
  error = signal('');

  canBook = computed(() => !!this.patient() && !!this.doctor() && !!this.date() && !!this.time());

  bookAppointment = () => {
    if (!this.canBook() || this.isSaving()) {
      return;
    }
    const body = this.getAppointmentBookingRequest();

    this.isSaving.set(true);
    this.error.set('');

    this.appointmentService.createAppointment(body).subscribe({
      next: (response) => {
        this.snackbar.open(`${this.patient()!.fullname} booked`, 'Dismiss', { duration: 5000 });

        this.dialogRef.close({
          id: response!.id,
          patientName: this.patient()!.fullname,
          startAt: body.startAt,
        });
      },
      error: (error: ApiError) => {
        this.isSaving.set(false);
        this.error.set(error.message);
      }
    });
  }

  /**
   * Build Appointment Booking Request Body
   */
  getAppointmentBookingRequest = (): CreateAppointmentRequest => {
    const [hours, minutes] = this.time().split(':').map(Number);
    const startAt = new Date(this.date()!);

    startAt.setHours(hours, minutes, 0, 0);

    const endAt = new Date(startAt.getTime() + this.minutes() * 60_000);

    return {
      patientId: this.patient()!.id,
      doctorId: this.doctor()!.id,
      startAt: startAt.toISOString(),
      endAt: endAt.toISOString()
    };
  }
}
