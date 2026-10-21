import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { formatDate } from '@angular/common';
import { MatButton } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MAT_DIALOG_DATA, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogRef, MatDialogTitle } from '@angular/material/dialog';
import { MatError, MatFormField, MatHint, MatLabel } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatInput } from '@angular/material/input';
import { MatOption, MatSelect } from '@angular/material/select';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { disabled, email, form, FormField, max, maxLength, min, minLength, readonly, required } from '@angular/forms/signals';

import { Observable } from 'rxjs';

import { DoctorService } from '@shared/services/doctor/doctor.service';

import { ApiError } from '@core/auth/types/auth.types';
import { Doctor } from '@shared/services/doctor/doctor.types';
import { DoctorFormData } from '../../types/doctors.types';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormField,
    MatButton,
    MatDatepickerModule,
    MatDialogActions,
    MatDialogClose,
    MatDialogContent,
    MatDialogTitle,
    MatError,
    MatFormField,
    MatHint,
    MatIcon,
    MatInput,
    MatLabel,
    MatOption,
    MatProgressSpinner,
    MatSelect,
  ],
  selector: 'app-doctor-form-dialog',
  styleUrl: './doctor-form-dialog.component.scss',
  templateUrl: './doctor-form-dialog.component.html',
})
export class DoctorFormDialogComponent {
  dialogRef = inject(MatDialogRef<DoctorFormDialogComponent>);
  doctorService = inject(DoctorService);
  snackbar = inject(MatSnackBar);
  existingDoctor = inject<Doctor | null>(MAT_DIALOG_DATA, { optional: true });

  readonly isEditing = !!this.existingDoctor;

  isSaving = signal(false);
  error = signal('');

  readonly MIN_YEAR = 1900;
  readonly MAX_YEAR = new Date().getFullYear();

  doctorModel = signal<DoctorFormData>({
    firstname: this.existingDoctor?.firstname ?? '',
    midname: this.existingDoctor?.midname ?? '',
    thirdname: this.existingDoctor?.thirdname ?? '',
    lastname: this.existingDoctor?.lastname ?? '',
    nationalNo: this.existingDoctor?.nationalNo ?? '',
    phone: this.existingDoctor?.phone ?? '',
    gender: this.existingDoctor?.gender ?? null,
    dateOfBirth: this.existingDoctor?.dateOfBirth
      ? new Date(`${this.existingDoctor.dateOfBirth}T00:00:00`)
      : null,
    specialization: this.existingDoctor?.specialization ?? '',
    medicalSchool: this.existingDoctor?.medicalSchool ?? '',
    graduationYear: this.existingDoctor?.graduationYear ?? null,
    email: '',
    password: '',
  });

  doctorForm = form(this.doctorModel, (schema) => {
    required(schema.firstname);
    maxLength(schema.firstname, 20);

    required(schema.midname);
    maxLength(schema.midname, 20);

    maxLength(schema.thirdname, 20);

    required(schema.lastname);
    maxLength(schema.lastname, 20);

    required(schema.phone);
    maxLength(schema.phone, 15);

    required(schema.gender);

    required(schema.dateOfBirth);
    readonly(schema.dateOfBirth);

    required(schema.specialization);
    maxLength(schema.specialization, 30);

    required(schema.medicalSchool);
    maxLength(schema.medicalSchool, 150);

    required(schema.graduationYear);
    min(schema.graduationYear, 1900);
    max(schema.graduationYear, this.MAX_YEAR);

    disabled(schema.nationalNo, { when: () => this.isEditing });
    readonly(schema.email, { when: () => this.isEditing });
    readonly(schema.password, { when: () => this.isEditing });

    required(schema.nationalNo, { when: () => !this.isEditing });
    maxLength(schema.nationalNo, 15);

    required(schema.email, { when: () => !this.isEditing });
    email(schema.email, { when: () => !this.isEditing });

    required(schema.password, { when: () => !this.isEditing });
    minLength(schema.password, 6, { when: () => !this.isEditing });
  });

  /**
   * Sends the form, then closes truthy so the screen reloads its list.
   *
   * Editing cannot reach the person's national number, email or password, so
   * the two bodies differ by more than a few fields.
   */
  saveDoctor = (event: Event): void => {
    event.preventDefault();

    if (this.doctorForm().invalid()) {
      this.doctorForm().markAsTouched();
      return;
    }

    if (this.isSaving()) {
      return;
    }

    this.isSaving.set(true);
    this.error.set('');

    const value = this.doctorForm().value();
    const person = {
      firstname: value.firstname.trim(),
      midname: value.midname.trim(),
      thirdname: value.thirdname.trim() || undefined,
      lastname: value.lastname.trim(),
      phone: value.phone.trim(),
      gender: value.gender!,
      dateOfBirth: formatDate(value.dateOfBirth!, 'yyyy-MM-dd', 'en-US'),
      specialization: value.specialization.trim(),
      medicalSchool: value.medicalSchool.trim(),
      graduationYear: value.graduationYear!,
    };

    const request: Observable<unknown> = this.existingDoctor
      ? this.doctorService.updateDoctor(this.existingDoctor.id, person)
      : this.doctorService.createDoctor({
        ...person,
        nationalNo: value.nationalNo.trim(),
        email: value.email.trim(),
        password: value.password,
      });

    request.subscribe({
      next: () => {
        this.snackbar.open(this.isEditing ? 'Doctor updated' : 'Doctor added', 'Dismiss', { duration: 5000 });
        this.dialogRef.close(true);
      },
      error: (error: ApiError) => {
        this.isSaving.set(false);
        this.error.set(error.message);
      },
    });
  }
}
