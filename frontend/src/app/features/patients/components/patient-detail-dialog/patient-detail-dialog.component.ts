import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogRef, MatDialogTitle } from '@angular/material/dialog';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { Router } from '@angular/router';

import { AuthService } from '@core/auth/service/auth.service';
import { PatientService } from '@shared/services/patient/patient.service';
import { VisitService } from '@shared/services/visit/visit.service';

import { PaginationComponent } from '@shared/components/pagination/pagination.component';

import { Patient } from '@shared/services/patient/patient.types';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, MatButton, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogTitle, MatIcon, MatProgressSpinner, PaginationComponent],
  selector: 'app-patient-detail-dialog',
  styleUrl: './patient-detail-dialog.component.scss',
  templateUrl: './patient-detail-dialog.component.html',
})
export class PatientDetailDialogComponent {
  authService = inject(AuthService);
  dialogRef = inject(MatDialogRef<PatientDetailDialogComponent>);
  patientService = inject(PatientService);
  router = inject(Router);
  visitService = inject(VisitService);

  patientData = inject<Patient>(MAT_DIALOG_DATA);

  showVisits = signal(false);
  visitsPage = signal(1);

  visitsResource = this.visitService.getPatientVisitResource(
    () => this.showVisits() ? this.patientData.id : null,
    this.visitsPage,
  );
  patientResource = this.patientService.getPatientByIdResource(this.patientData.id);

  /**
   * Open visit page
   *
   * @param visitId The visit that was picked.
   */
  openVisit(visitId: number): void {
    this.dialogRef.close();
    this.router.navigate(['/dashboard/visits'], { queryParams: { visitId } });
  }
}
