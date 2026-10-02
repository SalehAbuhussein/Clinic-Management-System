import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogTitle } from '@angular/material/dialog';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatProgressSpinner } from '@angular/material/progress-spinner';

import { DiagnosisService } from '@shared/services/diagnosis/diagnosis.service';

import { ApiError } from '@core/auth/types/auth.types';
import { Diagnosis } from '@shared/services/diagnosis/diagnosis.types';
import { Visit } from '@shared/services/visit/visit.types';

/**
 * What the dialog closes with. The screen owns every one of these, because each
 * opens another dialog and two stacked dialogs read badly.
 */
export type VisitDetailAction = 'diagnose' | 'edit' | 'checkout' | 'delete';

@Component({
  imports: [DatePipe, MatButton, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogTitle, MatIcon, MatProgressSpinner],
  selector: 'app-visit-detail-dialog',
  styleUrl: './visit-detail-dialog.component.scss',
  templateUrl: './visit-detail-dialog.component.html',
})
export class VisitDetailDialogComponent implements OnInit {
  diagnosisService = inject(DiagnosisService);

  visit = inject<Visit>(MAT_DIALOG_DATA);

  diagnosis = signal<Diagnosis | null>(null);

  isLoadingDiagnosis = signal(true);
  diagnosisError = signal('');

  /** Checking out locks the visit, and the API then refuses every change. */
  isOpen = computed(() => !this.visit.checkoutTime);

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
   * Reads the visit's diagnosis. A 404 means the doctor has not written one,
   * which is the normal state of a visit that just opened.
   */
  loadDiagnosis(): void {
    this.diagnosisService.getDiagnosis(this.visit.id).subscribe({
      next: (found) => {
        this.diagnosis.set(found);
        this.isLoadingDiagnosis.set(false);
      },
      error: (error: ApiError) => {
        this.isLoadingDiagnosis.set(false);

        if (error.status === 404) {
          return;
        }

        this.diagnosisError.set(error.message);
      },
    });
  }
}
