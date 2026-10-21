import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogTitle } from '@angular/material/dialog';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatProgressSpinner } from '@angular/material/progress-spinner';

import { AuthService } from '@core/auth/service/auth.service';
import { DiagnosisService } from '@shared/services/diagnosis/diagnosis.service';
import { PrescriptionService } from '@shared/services/prescription/prescription.service';

import { ApiError } from '@core/auth/types/auth.types';
import { Visit } from '@shared/services/visit/visit.types';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, MatButton, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogTitle, MatIcon, MatProgressSpinner],
  selector: 'app-visit-detail-dialog',
  styleUrl: './visit-detail-dialog.component.scss',
  templateUrl: './visit-detail-dialog.component.html',
})
export class VisitDetailDialogComponent {
  authService = inject(AuthService);
  diagnosisService = inject(DiagnosisService);
  prescriptionService = inject(PrescriptionService);

  visit = inject<Visit>(MAT_DIALOG_DATA);

  diagnosisResource = this.diagnosisService.getDiagnosisResource(() => this.visit.id);
  prescriptionResource = this.prescriptionService.getPrescriptionResource(() => this.visit.id);

  diagnosis = computed(() => this.diagnosisResource.value() ?? null);
  isLoadingDiagnosis = this.diagnosisResource.isLoading;

  /**
   * A 404 is the normal state of a visit whose doctor has not written a
   * diagnosis yet, so it reads as an empty section rather than a failure.
   */
  diagnosisError = computed(() => {
    const failure = this.diagnosisResource.error() as ApiError | undefined;

    return failure && failure.status !== 404 ? failure.message : '';
  });

  prescription = computed(() => this.prescriptionResource.value().items);
  isLoadingPrescription = this.prescriptionResource.isLoading;
  prescriptionError = computed(() => (this.prescriptionResource.error() as ApiError | undefined)?.message ?? '');

  /** Checking out locks the visit, and the API then refuses every change. */
  isOpen = computed(() => !this.visit.checkoutTime);

}
