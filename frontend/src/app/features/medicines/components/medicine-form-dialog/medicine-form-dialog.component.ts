import { Component, inject, signal } from '@angular/core';
import { MatButton } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogRef, MatDialogTitle } from '@angular/material/dialog';
import { MatError, MatFormField, MatHint, MatLabel } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatInput } from '@angular/material/input';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { disabled, form, FormField, maxLength, min, required } from '@angular/forms/signals';

import { Observable } from 'rxjs';

import { MedicineService } from '@shared/services/medicine/medicine.service';

import { ApiError } from '@core/auth/types/auth.types';
import { Medicine } from '@shared/services/medicine/medicine.types';
import { MedicineFormData } from '@features/medicines/types/medicines.types';

@Component({
  imports: [
    FormField,
    MatButton,
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
    MatProgressSpinner,
  ],
  selector: 'app-medicine-form-dialog',
  styleUrl: './medicine-form-dialog.component.scss',
  templateUrl: './medicine-form-dialog.component.html',
})
export class MedicineFormDialogComponent {
  dialogRef = inject(MatDialogRef<MedicineFormDialogComponent>);
  medicineService = inject(MedicineService);
  snackbar = inject(MatSnackBar);
  existingMedicine = inject<Medicine | null>(MAT_DIALOG_DATA, { optional: true });

  readonly isEditing = !!this.existingMedicine;

  isSaving = signal(false);
  error = signal('');

  medicineModel = signal<MedicineFormData>({
    rxCUI: this.existingMedicine?.rxCUI ?? null,
    genericname: this.existingMedicine?.genericname ?? '',
    brandname: this.existingMedicine?.brandname ?? '',
    form: this.existingMedicine?.form ?? '',
    strength: this.existingMedicine?.strength ?? '',
  });

  medicineForm = form(this.medicineModel, (schema) => {
    disabled(schema.rxCUI, { when: () => this.isEditing });
    required(schema.rxCUI, { when: () => !this.isEditing });
    min(schema.rxCUI, 1);

    required(schema.genericname);
    maxLength(schema.genericname, 3000);

    maxLength(schema.brandname, 255);

    required(schema.form);
    maxLength(schema.form, 100);

    required(schema.strength);
    maxLength(schema.strength, 255);
  });

  /**
   * Sends the form, then closes truthy so the screen reloads its list.
   */
  saveMedicine = (event: Event): void => {
    event.preventDefault();

    if (this.medicineForm().invalid()) {
      this.medicineForm().markAsTouched();
      return;
    }

    if (this.isSaving()) {
      return;
    }

    this.isSaving.set(true);
    this.error.set('');

    const value = this.medicineForm().value();
    const details = {
      genericname: value.genericname.trim(),
      brandname: value.brandname.trim() || undefined,
      form: value.form.trim(),
      strength: value.strength.trim(),
    };

    const request: Observable<unknown> = this.existingMedicine
      ? this.medicineService.updateMedicine(this.existingMedicine.id, details)
      : this.medicineService.createMedicine({ ...details, rxCUI: value.rxCUI! });

    request.subscribe({
      next: () => {
        this.snackbar.open(this.isEditing ? 'Medicine updated' : 'Medicine added', 'Dismiss', { duration: 5000 });
        this.dialogRef.close(true);
      },
      error: (error: ApiError) => {
        this.isSaving.set(false);
        this.error.set(error.message);
      },
    });
  }
}
