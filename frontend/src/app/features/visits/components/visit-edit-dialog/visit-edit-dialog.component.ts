import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
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
export class VisitEditDialogComponent implements OnInit {
  dialogRef = inject(MatDialogRef<VisitEditDialogComponent>);
  snackbar = inject(MatSnackBar);
  visitService = inject(VisitService);

  visit = inject<Visit>(MAT_DIALOG_DATA);

  checkinTime = signal('');
  notes = signal('');

  isSaving = signal(false);
  error = signal('');

  canSave = computed(() => !!this.checkinTime());

  ngOnInit(): void {
    this.initComponent();
  }

  /**
   * Everything the dialog needs before its first render.
   */
  initComponent(): void {
    this.prefillFromVisit();
  }

  /**
   * Fills the form from the visit. The stored instant is rendered in the
   * browser's own zone, because that is what the input shows and edits.
   */
  prefillFromVisit(): void {
    this.checkinTime.set(formatDate(this.visit.checkinTime, "yyyy-MM-dd'T'HH:mm", 'en-US'));
    this.notes.set(this.visit.notes ?? '');
  }

  /**
   * Saves the correction. The API answers 204, so nothing comes back to read.
   */
  saveVisit = () => {
    if (!this.canSave() || this.isSaving()) {
      return;
    }

    this.isSaving.set(true);
    this.error.set('');

    this.visitService.updateVisit(this.visit.id, {
      checkinTime: formatDate(this.checkinTime(), "yyyy-MM-dd'T'HH:mm:ssZZZZZ", 'en-US'),
      notes: this.notes().trim() || undefined,
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
