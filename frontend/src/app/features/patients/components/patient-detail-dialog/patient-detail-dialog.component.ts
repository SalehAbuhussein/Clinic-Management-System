import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialog, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogTitle } from '@angular/material/dialog';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatProgressSpinner } from '@angular/material/progress-spinner';

import { AuthService } from '@core/auth/service/auth.service';
import { PatientService } from '@shared/services/patient/patient.service';
import { VisitService } from '@shared/services/visit/visit.service';

import { PaginationComponent } from '@shared/components/pagination/pagination.component';
import { VisitDetailDialogComponent } from '@features/visits/components/visit-detail-dialog/visit-detail-dialog.component';

import { Patient } from '@shared/services/patient/patient.types';
import { Visit } from '@shared/services/visit/visit.types';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, MatButton, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogTitle, MatIcon, MatProgressSpinner, PaginationComponent],
  selector: 'app-patient-detail-dialog',
  styleUrl: './patient-detail-dialog.component.scss',
  templateUrl: './patient-detail-dialog.component.html',
})
export class PatientDetailDialogComponent {
  authService = inject(AuthService);
  dialog = inject(MatDialog);
  patientService = inject(PatientService);
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
   * Open Visit dialog in read only mode
   *
   * @param visit The visit that was picked.
   */
  openVisit(visit: Visit): void {
    this.dialog.open(VisitDetailDialogComponent, { width: '520px', data: { visit, readOnly: true } });
  }
}
