import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogRef, MatDialogTitle } from '@angular/material/dialog';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatInput } from '@angular/material/input';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';

import { Observable } from 'rxjs';

import { PrescriptionService } from '@shared/services/prescription/prescription.service';

import { MedicineSelectorInputComponent } from '../medicine-selector-input/medicine-selector-input.component';

import { ApiError } from '@core/auth/types/auth.types';
import { Medicine } from '@shared/services/medicine/medicine.types';
import { PrescriptionItem, PrescriptionRequest } from '@shared/services/prescription/prescription.types';
import { Visit } from '@shared/services/visit/visit.types';

/**
 * One editable line. The key is what @for tracks, so removing a line does not
 * shuffle the values of the ones below it.
 */
export type PrescriptionLine = {
  key: number;
  medicine: Medicine | null;
  dosage: string;
  frequency: string;
  duration: string;
};

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    MatButton,
    MatDialogActions,
    MatDialogClose,
    MatDialogContent,
    MatDialogTitle,
    MatFormField,
    MatIcon,
    MatIconButton,
    MatInput,
    MatLabel,
    MatProgressSpinner,
    MedicineSelectorInputComponent,
  ],
  selector: 'app-prescription-dialog',
  styleUrl: './prescription-dialog.component.scss',
  templateUrl: './prescription-dialog.component.html',
})
export class PrescriptionDialogComponent implements OnInit {
  dialogRef = inject(MatDialogRef<PrescriptionDialogComponent>);
  prescriptionService = inject(PrescriptionService);
  snackbar = inject(MatSnackBar);

  visit = inject<Visit>(MAT_DIALOG_DATA);

  /** Whether the visit already had a prescription, which decides POST or PUT. */
  existed = signal(false);

  loadFailed = signal(false);

  lines = signal<PrescriptionLine[]>([]);

  isLoading = signal(true);
  isSaving = signal(false);
  error = signal('');

  /** Rises forever, so a removed line's key is never reused. */
  nextKey = 0;

  /** The API rejects the same medicine twice, so the form catches it first. */
  duplicateMedicineIds = computed(() => {
    const seen = new Set<number>();
    const twice = new Set<number>();

    for (const line of this.lines()) {
      if (!line.medicine) {
        continue;
      }

      if (seen.has(line.medicine.id)) {
        twice.add(line.medicine.id);
      }

      seen.add(line.medicine.id);
    }

    return twice;
  });

  canSave = computed(() => this.lines().length > 0
    && this.duplicateMedicineIds().size === 0
    && this.lines().every((line) => !!line.medicine && !!line.dosage.trim() && !!line.frequency.trim() && !!line.duration.trim()));

  ngOnInit(): void {
    this.initComponent();
  }

  /**
   * Everything the dialog needs before its first render.
   */
  initComponent(): void {
    this.loadPrescription();
  }

  /**
   * Reads the visit's prescription. An empty array is the normal state of a
   * visit nobody has prescribed for, so it starts with one blank line.
   */
  loadPrescription(): void {
    this.prescriptionService.getPrescription(this.visit.id).subscribe({
      next: (found) => {
        this.existed.set(found.length > 0);
        this.lines.set(found.length ? found.map((item) => this.toLine(item)) : [this.blankLine()]);
        this.isLoading.set(false);
      },
      error: (error: ApiError) => {
        this.isLoading.set(false);
        this.loadFailed.set(true);
        this.error.set(error.message);
      },
    });
  }

  /**
   * Turns a saved line into an editable one. The medicine is rebuilt from the
   * joined columns rather than fetched, because the list already carries them.
   *
   * @param item One saved prescription line.
   */
  toLine = (item: PrescriptionItem): PrescriptionLine => ({
    key: this.nextKey++,
    medicine: {
      id: item.medicineID,
      rxCUI: 0,
      genericname: item.genericname,
      brandname: item.brandname ?? '',
      form: item.form,
      strength: item.strength,
    },
    dosage: item.dosage,
    frequency: item.frequency,
    duration: item.duration,
  });

  blankLine = (): PrescriptionLine => ({ key: this.nextKey++, medicine: null, dosage: '', frequency: '', duration: '' });

  addLine = () => this.lines.update((lines) => [...lines, this.blankLine()]);

  removeLine = (key: number) => this.lines.update((lines) => lines.filter((line) => line.key !== key));

  /**
   * @param key The line that changed.
   * @param medicine What was picked.
   */
  setMedicine = (key: number, medicine: Medicine | null) =>
    this.lines.update((lines) => lines.map((line) => line.key === key ? { ...line, medicine } : line));

  /**
   * Submits the whole set. The update is a replace, so anything left out is
   * deleted; that is the API's own semantic, not a shortcut here.
   */
  savePrescription = () => {
    if (!this.canSave() || this.isSaving()) {
      return;
    }

    this.isSaving.set(true);
    this.error.set('');

    const body: PrescriptionRequest = {
      items: this.lines().map((line) => ({
        medicineId: line.medicine!.id,
        dosage: line.dosage.trim(),
        frequency: line.frequency.trim(),
        duration: line.duration.trim(),
      })),
    };

    // The create answers 201 with no body and the replace answers 204, so the
    // two calls have different value types. Only success matters here.
    const request: Observable<unknown> = this.existed()
      ? this.prescriptionService.updatePrescription(this.visit.id, body)
      : this.prescriptionService.createPrescription(this.visit.id, body);

    request.subscribe({
      next: () => {
        this.snackbar.open('Prescription saved', 'Dismiss', { duration: 4000 });
        this.dialogRef.close(true);
      },
      error: (error: ApiError) => {
        this.isSaving.set(false);
        this.error.set(error.message);
      },
    });
  }
}
