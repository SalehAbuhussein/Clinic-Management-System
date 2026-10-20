import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogTitle } from '@angular/material/dialog';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatProgressSpinner } from '@angular/material/progress-spinner';

import { AuthService } from '@core/auth/service/auth.service';
import { DiagnosisService } from '@shared/services/diagnosis/diagnosis.service';
import { PrescriptionService } from '@shared/services/prescription/prescription.service';

import { ApiError } from '@core/auth/types/auth.types';
import { Diagnosis } from '@shared/services/diagnosis/diagnosis.types';
import { PrescriptionItem } from '@shared/services/prescription/prescription.types';
import { Visit } from '@shared/services/visit/visit.types';

@Component({
  imports: [DatePipe, MatButton, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogTitle, MatIcon, MatProgressSpinner],
  selector: 'app-visit-detail-dialog',
  styleUrl: './visit-detail-dialog.component.scss',
  templateUrl: './visit-detail-dialog.component.html',
})
export class VisitDetailDialogComponent implements OnInit {
  authService = inject(AuthService);
  diagnosisService = inject(DiagnosisService);
  prescriptionService = inject(PrescriptionService);

  visit = inject<Visit>(MAT_DIALOG_DATA);

  diagnosis = signal<Diagnosis | null>(null);

  isLoadingDiagnosis = signal(true);
  diagnosisError = signal('');

  prescription = signal<PrescriptionItem[]>([]);

  isLoadingPrescription = signal(true);
  prescriptionError = signal('');

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
    this.loadPrescription();
  }

  /**
   * Reads the visit's prescription. An empty array means nobody wrote one,
   * which the API reports with a 200 rather than a 404.
   */
  loadPrescription(): void {
    this.prescriptionService.getPrescription(this.visit.id).subscribe({
      next: (found) => {
        this.prescription.set(found);
        this.isLoadingPrescription.set(false);
      },
      error: (error: ApiError) => {
        this.isLoadingPrescription.set(false);
        this.prescriptionError.set(error.message);
      },
    });
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
