import { ActivatedRoute, Router } from '@angular/router';
import { ChangeDetectionStrategy, Component, computed, effect, inject, linkedSignal, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButton } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormField, MatLabel, MatPrefix } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatInput } from '@angular/material/input';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';

import { AuthService } from '@core/auth/service/auth.service';
import { PatientService } from '@shared/services/patient/patient.service';

import { pageFromUrl } from '@core/utils/url-state.utils';
import { ConfirmDialogComponent } from '@shared/components/confirm-dialog/confirm-dialog.component';
import { ExportButtonComponent } from '@shared/components/export-button/export-button.component';
import { PaginationComponent } from '@shared/components/pagination/pagination.component';
import { PatientDetailDialogComponent } from './components/patient-detail-dialog/patient-detail-dialog.component';
import { PatientFormDialogComponent } from './components/patient-form-dialog/patient-form-dialog.component';
import { PatientsListComponent } from './components/patients-list/patients-list.component';

import { ApiError } from '@core/auth/types/auth.types';
import { ConfirmDialogData } from '@shared/components/confirm-dialog/confirm-dialog.types';
import { Patient, PatientFilters } from '@shared/services/patient/patient.types';
import { PatientDetailAction } from './types/patients.types';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ExportButtonComponent,
    FormsModule,
    MatButton,
    MatFormField,
    MatIcon,
    MatInput,
    MatLabel,
    MatPrefix,
    MatProgressSpinner,
    PaginationComponent,
    PatientsListComponent,
  ],
  selector: 'app-patients',
  styleUrl: './patients.component.scss',
  templateUrl: './patients.component.html',
})
export class PatientsComponent {
  authService = inject(AuthService);
  dialog = inject(MatDialog);
  patientService = inject(PatientService);
  route = inject(ActivatedRoute);
  router = inject(Router);
  snackbar = inject(MatSnackBar);

  fullname = signal(this.route.snapshot.queryParamMap.get('fullname') ?? '');
  phone = signal(this.route.snapshot.queryParamMap.get('phone') ?? '');

  /**
   * Each field, held back until typing pauses. Signals carry no notion of time,
   * so the delay is an RxJS operator and the value crosses back.
   *
   * Debouncing the two strings separately rather than one object matters:
   * toObservable re-emits its current value on subscribe, so a signal holding
   * an object would take a second, identical object and count as changed. A
   * string compares equal to itself and the duplicate dies here.
   */
  debouncedFullname = toSignal(
    toObservable(this.fullname).pipe(debounceTime(300), distinctUntilChanged()),
    { initialValue: this.fullname() },
  );

  debouncedPhone = toSignal(
    toObservable(this.phone).pipe(debounceTime(300), distinctUntilChanged()),
    { initialValue: this.phone() },
  );

  /** A computed only re-runs when a dependency really changed, so this object
   * is built once per actual search rather than once per emission. */
  filters = computed(() => ({ fullname: this.debouncedFullname().trim(), phone: this.debouncedPhone().trim() }));

  page = linkedSignal<PatientFilters, number>({
    source: this.filters,
    computation: (_, previous) => previous ? 1 : pageFromUrl(this.route),
  });

  patientsResource = this.patientService.getPatientResource(() => ({
    fullname: this.filters().fullname,
    phone: this.filters().phone,
    page: this.page(),
  }));

  patients = computed(() => this.patientsResource.value().items);
  total = computed(() => this.patientsResource.value().total);
  perPage = computed(() => this.patientsResource.value().perPage || 10);

  isSearching = computed(() => !!this.filters().fullname || !!this.filters().phone);

  constructor() {
    effect(() => {
      this.router.navigate([], {
        queryParams: {
          fullname: this.filters().fullname || null,
          phone: this.filters().phone || null,
          page: this.page(),
        },
        relativeTo: this.route,
        replaceUrl: true,
      });
    });
  }

  /**
   * Show everything on a patient, and act on whatever the dialog closes with.
   *
   * Both actions open another dialog, so the detail closes first rather than
   * stacking two on top of each other.
   *
   * @param patient The row that was picked.
   */
  openPatient = (patient: Patient) => {
    this.dialog.open(PatientDetailDialogComponent, { width: '480px', data: patient })
      .afterClosed()
      .subscribe((action: PatientDetailAction | undefined) => {
        switch (action) {
          case 'edit':
            this.editPatient(patient);
            break;
          case 'delete':
            this.deletePatient(patient);
            break;
        }
      });
  }

  /**
   * Registers a patient, then shows them in the list.
   */
  openNewPatientDialog = () => this.openFormDialog(null);

  /**
   * Edits a patient. The row carries only the list's columns, so the dialog is
   * opened with the full record fetched by id.
   *
   * @param patient The row that was picked.
   */
  editPatient = (patient: Patient) => {
    this.patientService.getPatient(patient.id).subscribe({
      next: (full) => this.openFormDialog(full),
      error: (error: ApiError) => this.snackbar.open(error.message, 'Dismiss', { duration: 8000 }),
    });
  }

  /**
   * Opens the form and refreshes the list when it saved.
   *
   * @param patient The patient to edit, or null to register one.
   */
  openFormDialog = (patient: Patient | null) => {
    this.dialog.open(PatientFormDialogComponent, { width: '560px', data: patient })
      .afterClosed()
      .subscribe((saved: boolean | undefined) => {
        if (!saved) {
          return;
        }

        this.patientsResource.reload();
      });
  }

  /**
   * Deletes a patient, once. It takes their history with them, so it asks.
   *
   * @param patient The row that was picked.
   */
  deletePatient = (patient: Patient) => {
    this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        title: `Delete ${patient.fullname}?`,
        message: 'The record goes for good, along with anything that points at it. This cannot be undone.',
        confirmLabel: 'Delete patient',
        dismissLabel: 'Keep patient',
        destructive: true,
      } satisfies ConfirmDialogData,
    })
    .afterClosed()
    .subscribe((confirmed: boolean | undefined) => {
      if (!confirmed) {
        return;
      }

      this.patientService.deletePatient(patient.id).subscribe({
        next: () => {
          this.snackbar.open(`${patient.fullname} deleted`, 'Dismiss', { duration: 4000 });
          this.patientsResource.reload();
        },
        error: (error: ApiError) => this.snackbar.open(error.message, 'Dismiss', { duration: 8000 }),
      });
    });
  }

  /**
   * Clears both filters, which is one action rather than two empty fields.
   */
  clearFilters = () => {
    this.fullname.set('');
    this.phone.set('');
  }
}
