import { Component, computed, inject, OnInit, signal } from '@angular/core';
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

import { ApiError } from '@core/auth/types/auth.types';
import { Patient } from '@shared/services/patient/patient.types';

/**
 * What the dialog closes with. Both open another dialog, so the screen owns
 * them rather than stacking two on top of each other.
 */
export type PatientDetailAction = 'edit' | 'delete';

@Component({
  imports: [DatePipe, MatButton, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogTitle, MatIcon, MatProgressSpinner, PaginationComponent],
  selector: 'app-patient-detail-dialog',
  styleUrl: './patient-detail-dialog.component.scss',
  templateUrl: './patient-detail-dialog.component.html',
})
export class PatientDetailDialogComponent implements OnInit {
  authService = inject(AuthService);
  dialogRef = inject(MatDialogRef<PatientDetailDialogComponent>);
  patientService = inject(PatientService);
  router = inject(Router);
  visitService = inject(VisitService);

  /** The row that was picked. It carries the list's columns and nothing more. */
  row = inject<Patient>(MAT_DIALOG_DATA);

  /** The whole record, which only the single-patient endpoint returns. */
  patient = signal<Patient | null>(null);

  isLoading = signal(true);
  error = signal('');

  /** The history is fetched only once someone asks for it. */
  showVisits = signal(false);
  visitsPage = signal(1);

  visitsResource = this.visitService.getPatientVisitResource(
    () => this.showVisits() ? this.row.id : null,
    this.visitsPage,
  );

  visits = computed(() => this.visitsResource.value().items);
  visitsTotal = computed(() => this.visitsResource.value().total);
  visitsPerPage = computed(() => this.visitsResource.value().perPage || 10);

  ngOnInit(): void {
    this.initComponent();
  }

  /**
   * Everything the dialog needs before its first render.
   */
  initComponent(): void {
    this.loadPatient();
  }

  /**
   * Reads the full record. The list returns a name and a phone, so the national
   * number, gender and date of birth have to come from here.
   */
  loadPatient(): void {
    this.patientService.getPatient(this.row.id).subscribe({
      next: (found) => {
        this.patient.set(found);
        this.isLoading.set(false);
      },
      error: (error: ApiError) => {
        this.isLoading.set(false);
        this.error.set(error.message);
      },
    });
  }

  /**
   * Opens one of the patient's visits on the visits screen, which owns every
   * action a visit has. The dialog closes first, because the page behind it
   * is about to change.
   *
   * @param visitId The visit that was picked.
   */
  openVisit(visitId: number): void {
    this.dialogRef.close();
    this.router.navigate(['/dashboard/visits'], { queryParams: { visitId } });
  }
}
