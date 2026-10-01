import { ActivatedRoute, Router } from '@angular/router';
import { Component, computed, effect, inject, linkedSignal, signal } from '@angular/core';
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

import { PatientService } from '@shared/services/patient/patient.service';

import { ConfirmDialogComponent } from '@shared/components/confirm-dialog/confirm-dialog.component';
import { PaginationComponent } from '@shared/components/pagination/pagination.component';
import { PatientFormDialogComponent } from './components/patient-form-dialog/patient-form-dialog.component';
import { PatientsListComponent } from './components/patients-list/patients-list.component';

import { ApiError } from '@core/auth/types/auth.types';
import { ConfirmDialogData } from '@shared/components/confirm-dialog/confirm-dialog.types';
import { Patient, PatientFilters } from '@shared/services/patient/patient.types';

@Component({
  imports: [
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
  dialog = inject(MatDialog);
  patientService = inject(PatientService);
  route = inject(ActivatedRoute);
  router = inject(Router);
  snackbar = inject(MatSnackBar);

  fullname = signal(this.route.snapshot.queryParamMap.get('fullname') ?? '');
  phone = signal(this.route.snapshot.queryParamMap.get('phone') ?? '');

  /**
   * The filters, held back until typing pauses. Signals carry no notion of
   * time, so the delay is an RxJS operator and the values cross back.
   */
  filters = toSignal(
    toObservable(computed(() => ({ fullname: this.fullname().trim(), phone: this.phone().trim() })))
      .pipe(debounceTime(300), distinctUntilChanged((left, right) => left.fullname === right.fullname && left.phone === right.phone)),
    { initialValue: { fullname: this.fullname().trim(), phone: this.phone().trim() } },
  );

  // Resets to the first page on a new search, but takes the URL's page on the
  // first run so a reload or a shared link lands where it left off.
  page = linkedSignal<PatientFilters, number>({
    source: this.filters,
    computation: (_, previous) => previous ? 1 : this.pageFromUrl(),
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
   * @returns The page from the URL, or the first one.
   */
  pageFromUrl(): number {
    const page = Number(this.route.snapshot.queryParamMap.get('page'));

    return Number.isInteger(page) && page > 0 ? page : 1;
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
