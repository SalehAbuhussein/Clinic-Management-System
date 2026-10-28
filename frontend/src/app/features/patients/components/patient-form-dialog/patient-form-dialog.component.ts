import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { disabled, form, FormField, maxLength, required } from '@angular/forms/signals';
import { MAT_DIALOG_DATA, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogRef, MatDialogTitle } from '@angular/material/dialog';
import { MatButton } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormField, MatHint, MatLabel, MatSuffix } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatInput } from '@angular/material/input';
import { MatOption, MatSelect } from '@angular/material/select';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { formatDate } from '@angular/common';

import { PatientService } from '@shared/services/patient/patient.service';

import { ApiError } from '@core/auth/types/auth.types';
import { CreatePatientRequest, Patient, PatientFormValue, UpdatePatientRequest } from '@shared/services/patient/patient.types';

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
    MatFormField,
    MatHint,
    MatIcon,
    MatInput,
    MatLabel,
    MatOption,
    MatProgressSpinner,
    MatSelect,
    MatSuffix,
  ],
  selector: 'app-patient-form-dialog',
  styleUrl: './patient-form-dialog.component.scss',
  templateUrl: './patient-form-dialog.component.html',
})
export class PatientFormDialogComponent {
  dialogRef = inject(MatDialogRef<PatientFormDialogComponent>);
  patientService = inject(PatientService);
  snackbar = inject(MatSnackBar);

  existingPatient = inject<Patient | null>(MAT_DIALOG_DATA, { optional: true });

  readonly isEditing = !!this.existingPatient;

  model = signal<PatientFormValue>({
    firstname: this.existingPatient?.firstname ?? '',
    midname: this.existingPatient?.midname ?? '',
    thirdname: this.existingPatient?.thirdname ?? '',
    lastname: this.existingPatient?.lastname ?? '',
    nationalNo: this.existingPatient?.nationalNo ?? '',
    phone: this.existingPatient?.phone ?? '',
    gender: this.existingPatient?.gender ?? null,
    dateOfBirth: this.existingPatient?.dateOfBirth
      ? new Date(this.existingPatient.dateOfBirth)
      : null,
    emergencyContactName: this.existingPatient?.emergencyContactName ?? '',
    emergencyContactNumber: this.existingPatient?.emergencyContactNumber ?? '',
  });

  patientForm = form(this.model, (patient) => {
    required(patient.firstname);
    required(patient.midname);
    required(patient.lastname);
    required(patient.phone);
    required(patient.gender);
    required(patient.dateOfBirth);

    maxLength(patient.firstname, 20);
    maxLength(patient.midname, 20);
    maxLength(patient.thirdname, 20);
    maxLength(patient.lastname, 20);
    maxLength(patient.nationalNo, 15);
    maxLength(patient.phone, 15);
    maxLength(patient.emergencyContactName, 50);
    maxLength(patient.emergencyContactNumber, 30);

    disabled(patient.nationalNo, {
      when: () => this.isEditing ? 'The national number identifies the record.' : false,
    });

    if (!this.isEditing) {
      required(patient.nationalNo);
    }
  });

  isSaving = signal(false);
  error = signal('');

  canSave = computed(() => this.patientForm().valid());

  /**
   * Submits the form. Which call it makes is the only thing the mode decides.
   */
  savePatient = () => {
    if (!this.canSave() || this.isSaving()) {
      return;
    }

    this.isSaving.set(true);
    this.error.set('');

    const existing = this.existingPatient;

    if (existing) {
      this.updatePatient(existing);
      return;
    }

    this.createPatient();
  }

  /**
   * Registers the patient. A national number already on file answers 409.
   */
  createPatient = (): void => {
    this.patientService.createPatient(this.getCreateRequest()).subscribe({
      next: () => this.finish('registered'),
      error: this.fail,
    });
  }

  /**
   * Saves the edits. The API answers 204, so nothing comes back to read.
   *
   * @param existing The patient being edited.
   */
  updatePatient = (existing: Patient): void => {
    this.patientService.updatePatient(existing.id, this.getUpdateRequest()).subscribe({
      next: () => this.finish('updated'),
      error: this.fail,
    });
  }

  /**
   * The person fields both calls take.
   */
  getPersonFields = (): UpdatePatientRequest => {
    const value = this.model();

    return {
      firstname: value.firstname.trim(),
      midname: value.midname.trim(),
      thirdname: value.thirdname.trim() || undefined,
      lastname: value.lastname.trim(),
      phone: value.phone.trim(),
      gender: value.gender!,
      dateOfBirth: formatDate(value.dateOfBirth!, 'yyyy-MM-dd', 'en-US'),
      emergencyContactName: value.emergencyContactName.trim() || undefined,
      emergencyContactNumber: value.emergencyContactNumber.trim() || undefined,
    };
  }

  /**
   * Build Create Patient Request Body
   */
  getCreateRequest = (): CreatePatientRequest => ({
    ...this.getPersonFields(),
    nationalNo: this.model().nationalNo.trim(),
  })

  /**
   * Build Update Patient Request Body. Carries no national number: the API
   * cannot change the value that identifies the record.
   */
  getUpdateRequest = (): UpdatePatientRequest => this.getPersonFields()

  /**
   * Confirms, then closes so the list knows to refresh.
   *
   * @param verb What happened, for the confirmation.
   */
  finish = (verb: 'registered' | 'updated'): void => {
    const value = this.model();

    this.snackbar.open(value.firstname + ' ' + value.lastname + ' ' + verb, 'Dismiss', { duration: 5000 });

    this.dialogRef.close(true);
  }

  /**
   * Leaves the form as it was and shows the server's own wording.
   *
   * @param error The mapped failure from the error interceptor.
   */
  fail = (error: ApiError): void => {
    this.isSaving.set(false);
    this.error.set(error.message);
  }
}
