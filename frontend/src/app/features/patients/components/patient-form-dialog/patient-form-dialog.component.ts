import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
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
import { CreatePatientRequest, Patient, UpdatePatientRequest } from '@shared/services/patient/patient.types';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
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
export class PatientFormDialogComponent implements OnInit {
  dialogRef = inject(MatDialogRef<PatientFormDialogComponent>);
  patientService = inject(PatientService);
  snackbar = inject(MatSnackBar);

  existingPatient = inject<Patient | null>(MAT_DIALOG_DATA, { optional: true });

  readonly isEditing = !!this.existingPatient;

  firstname = signal('');
  midname = signal('');
  thirdname = signal('');
  lastname = signal('');
  nationalNo = signal('');
  phone = signal('');
  gender = signal<boolean | null>(null);
  dateOfBirth = signal<Date | null>(null);
  emergencyContactName = signal('');
  emergencyContactNumber = signal('');

  isSaving = signal(false);
  error = signal('');

  canSave = computed(() => !!this.firstname().trim()
    && !!this.midname().trim()
    && !!this.lastname().trim()
    && !!this.phone().trim()
    && this.gender() !== null
    && !!this.dateOfBirth()
    && (this.isEditing || !!this.nationalNo().trim()));

  /**
   * Angular OnInit lifecycle method
   */
  ngOnInit(): void {
    this.initComponent();
  }

  /**
   * Everything the dialog needs before its first render.
   */
  initComponent(): void {
    this.prefillFromExisting();
  }

  /**
   * Fills the form from the patient being edited. Registering starts empty.
   */
  prefillFromExisting(): void {
    const existing = this.existingPatient;

    if (!existing) {
      return;
    }

    this.firstname.set(existing.firstname ?? '');
    this.midname.set(existing.midname ?? '');
    this.thirdname.set(existing.thirdname ?? '');
    this.lastname.set(existing.lastname ?? '');
    this.nationalNo.set(existing.nationalNo ?? '');
    this.phone.set(existing.phone ?? '');
    this.gender.set(existing.gender);
    this.dateOfBirth.set(existing.dateOfBirth ? new Date(`${existing.dateOfBirth}T00:00:00`) : null);
    this.emergencyContactName.set(existing.emergencyContactName ?? '');
    this.emergencyContactNumber.set(existing.emergencyContactNumber ?? '');
  }

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
  getPersonFields = (): UpdatePatientRequest => ({
    firstname: this.firstname().trim(),
    midname: this.midname().trim(),
    thirdname: this.thirdname().trim() || undefined,
    lastname: this.lastname().trim(),
    phone: this.phone().trim(),
    gender: this.gender()!,
    dateOfBirth: formatDate(this.dateOfBirth()!, 'yyyy-MM-dd', 'en-US'),
    emergencyContactName: this.emergencyContactName().trim() || undefined,
    emergencyContactNumber: this.emergencyContactNumber().trim() || undefined,
  })

  /**
   * Build Create Patient Request Body
   */
  getCreateRequest = (): CreatePatientRequest => ({
    ...this.getPersonFields(),
    nationalNo: this.nationalNo().trim(),
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
    this.snackbar.open(`${this.firstname()} ${this.lastname()} ${verb}`, 'Dismiss', { duration: 5000 });

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
