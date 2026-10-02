import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogRef, MatDialogTitle } from '@angular/material/dialog';
import { MatButton } from '@angular/material/button';
import { MatFormField, MatHint, MatLabel } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatInput } from '@angular/material/input';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';

import { Observable } from 'rxjs';

import { DiagnosisService } from '@shared/services/diagnosis/diagnosis.service';

import { ApiError } from '@core/auth/types/auth.types';
import { Diagnosis } from '@shared/services/diagnosis/diagnosis.types';
import { Visit } from '@shared/services/visit/visit.types';

@Component({
  imports: [
    FormsModule,
    MatButton,
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
  ],
  selector: 'app-diagnosis-dialog',
  styleUrl: './diagnosis-dialog.component.scss',
  templateUrl: './diagnosis-dialog.component.html',
})
export class DiagnosisDialogComponent implements OnInit {
  diagnosisService = inject(DiagnosisService);
  dialogRef = inject(MatDialogRef<DiagnosisDialogComponent>);
  snackbar = inject(MatSnackBar);

  visit = inject<Visit>(MAT_DIALOG_DATA);

  existing = signal<Diagnosis | null>(null);

  diagnosis = signal('');
  notes = signal('');

  isLoading = signal(true);
  isSaving = signal(false);
  error = signal('');

  canSave = computed(() => !!this.diagnosis().trim());

  /**
   * Angular onInit lifecycle method
   */
  ngOnInit(): void {
    this.initComponent();
  }

  /**
   * Everything the dialog needs before its first render.
   */
  initComponent(): void {
    this.loadDiagnosis();
  }

  /**
   * Reads the visit's diagnosis to decide which mode the form is in.
   *
   * A 404 here means the doctor has not written one yet, which is the normal
   * state of a visit that just opened. Only another status is a real failure.
   */
  loadDiagnosis(): void {
    this.diagnosisService.getDiagnosis(this.visit.id).subscribe({
      next: (found) => {
        this.existing.set(found);
        this.diagnosis.set(found.diagnosis);
        this.notes.set(found.notes ?? '');
        this.isLoading.set(false);
      },
      error: (error: ApiError) => {
        this.isLoading.set(false);

        if (error.status === 404) {
          return;
        }

        this.error.set(error.message);
      },
    });
  }

  /**
   * Submits the form. Which call it makes is the only thing the mode decides.
   */
  saveDiagnosis = () => {
    if (!this.canSave() || this.isSaving()) {
      return;
    }

    this.isSaving.set(true);
    this.error.set('');

    const body = {
      diagnosis: this.diagnosis().trim(),
      notes: this.notes().trim() || undefined,
    };

    const request: Observable<unknown> = this.existing()
      ? this.diagnosisService.updateDiagnosis(this.visit.id, body)
      : this.diagnosisService.createDiagnosis(this.visit.id, body);

    request.subscribe({
      next: () => {
        this.snackbar.open('Diagnosis saved', 'Dismiss', { duration: 4000 });
        this.dialogRef.close(true);
      },
      error: (error: ApiError) => {
        this.isSaving.set(false);
        this.error.set(error.message);
      },
    });
  }
}
