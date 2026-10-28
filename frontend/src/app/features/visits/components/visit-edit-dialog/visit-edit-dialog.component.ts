import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { form, FormField, maxLength, required } from '@angular/forms/signals';
import { MAT_DIALOG_DATA, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogRef, MatDialogTitle } from '@angular/material/dialog';
import { MatButton } from '@angular/material/button';
import { MatFormField, MatHint, MatLabel } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatInput } from '@angular/material/input';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { formatDate } from '@angular/common';

import { VisitService } from '@shared/services/visit/visit.service';

import { ApiError } from '@core/auth/types/auth.types';
import { Visit } from '@shared/services/visit/visit.types';
import { VisitEditFormValue } from '@features/visits/types/visits.types';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormField,
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
  selector: 'app-visit-edit-dialog',
  styleUrl: './visit-edit-dialog.component.scss',
  templateUrl: './visit-edit-dialog.component.html',
})
export class VisitEditDialogComponent {
  dialogRef = inject(MatDialogRef<VisitEditDialogComponent>);
  snackbar = inject(MatSnackBar);
  visitService = inject(VisitService);

  visit = inject<Visit>(MAT_DIALOG_DATA);

  model = signal<VisitEditFormValue>({
    checkinTime: formatDate(this.visit.checkinTime, "yyyy-MM-dd'T'HH:mm", 'en-US'),
    notes: this.visit.notes ?? '',
  });

  visitForm = form(this.model, (visit) => {
    required(visit.checkinTime);
    maxLength(visit.notes, 255);
  });

  isSaving = signal(false);
  error = signal('');

  canSave = computed(() => this.visitForm().valid());

  /**
   * Saves the correction. The API answers 204, so nothing comes back to read.
   */
  saveVisit = () => {
    if (!this.canSave() || this.isSaving()) {
      return;
    }

    this.isSaving.set(true);
    this.error.set('');

    const value = this.model();

    this.visitService.updateVisit(this.visit.id, {
      checkinTime: formatDate(value.checkinTime, "yyyy-MM-dd'T'HH:mm:ssZZZZZ", 'en-US'),
      notes: value.notes.trim() || undefined,
    }).subscribe({
      next: () => {
        this.snackbar.open('Visit updated', 'Dismiss', { duration: 4000 });
        this.dialogRef.close(true);
      },
      error: (error: ApiError) => {
        this.isSaving.set(false);
        this.error.set(error.message);
      },
    });
  }
}
