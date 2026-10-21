import { ChangeDetectionStrategy, Component, computed, inject, linkedSignal, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButton } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIcon } from '@angular/material/icon';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';

import { debounceTime, distinctUntilChanged } from 'rxjs';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';

import { filtersFromUrl, pageFromUrl, syncUrl } from '@core/utils/url-state.utils';
import { DoctorService } from '@shared/services/doctor/doctor.service';

import { ConfirmDialogComponent } from '@shared/components/confirm-dialog/confirm-dialog.component';
import { DoctorDetailDialogComponent } from './components/doctor-detail-dialog/doctor-detail-dialog.component';
import { DoctorFiltersComponent } from './components/doctor-filters/doctor-filters.component';
import { DoctorFormDialogComponent } from './components/doctor-form-dialog/doctor-form-dialog.component';
import { PaginationComponent } from '@shared/components/pagination/pagination.component';

import { ApiError } from '@core/auth/types/auth.types';
import { Doctor, DoctorFilters } from '@shared/services/doctor/doctor.types';
import { DoctorDetailAction } from './types/doctors.types';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DoctorFiltersComponent, MatButton, MatIcon, MatProgressSpinner, PaginationComponent],
  selector: 'app-doctors',
  styleUrl: './doctors.component.scss',
  templateUrl: './doctors.component.html',
})
export class DoctorsComponent {
  dialog = inject(MatDialog);
  doctorService = inject(DoctorService);
  route = inject(ActivatedRoute);
  router = inject(Router);
  snackbar = inject(MatSnackBar);

  filters = signal<DoctorFilters>(filtersFromUrl(this.route));

  /**
   * The filters, held back until typing pauses. Comparing the contents stops a
   * change of the field dropdown on an empty box from refetching a list that
   * would come back identical.
   */
  debouncedFilters = toSignal(
    toObservable(this.filters).pipe(
      debounceTime(300),
      distinctUntilChanged((one, other) => JSON.stringify(one) === JSON.stringify(other)),
    ),
    { initialValue: this.filters() },
  );

  // Back to the first page on a new search, but the URL's page on the first
  // run, so a reload or a shared link lands where it left off.
  page = linkedSignal<DoctorFilters, number>({
    source: this.debouncedFilters,
    computation: (_, previous) => previous ? 1 : pageFromUrl(this.route),
  });

  doctorsResource = this.doctorService.getDoctorResource(() => ({ ...this.debouncedFilters(), page: this.page() }));

  doctors = computed(() => this.doctorsResource.value().items);
  total = computed(() => this.doctorsResource.value().total);
  perPage = computed(() => this.doctorsResource.value().perPage || 10);

  constructor() {
    syncUrl(this.router, this.route, () => ({ ...this.filters(), page: this.page() }));
  }

  /**
   * Remove a doctor. Their appointments and visits still point at the row, so
   * the API refuses more often than it accepts; its wording says why.
   *
   * @param doctor The row that was picked.
   */
  deleteDoctor = (doctor: Doctor) => {
    this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        title: 'Delete this doctor?',
        message: `${doctor.fullname} and their login are removed. Appointments already booked with them are not.`,
        confirmLabel: 'Delete',
      },
    })
      .afterClosed()
      .subscribe((confirmed: boolean | undefined) => {
        if (!confirmed) {
          return;
        }

        this.doctorService.deleteDoctor(doctor.id).subscribe({
          next: () => {
            this.snackbar.open('Doctor deleted', 'Dismiss', { duration: 4000 });
            this.doctorsResource.reload();
          },
          error: (error: ApiError) => this.snackbar.open(error.message, 'Dismiss', { duration: 8000 }),
        });
      });
  }

  /**
   * Opens the form in whichever mode the caller wants and refreshes on a save.
   *
   * @param doctor The row being edited, or null to add one.
   */
  openDetail(doctor: Doctor): void {
    this.dialog.open(DoctorDetailDialogComponent, { width: '440px', data: doctor })
      .afterClosed()
      .subscribe((action: DoctorDetailAction | undefined) => {
        if (action === 'edit') {
          this.openForm(doctor);
        }
      });
  }

  openForm(doctor: Doctor | null): void {
    this.dialog.open(DoctorFormDialogComponent, { width: '640px', maxWidth: '100vw', data: doctor })
      .afterClosed()
      .subscribe((saved: boolean | undefined) => {
        if (!saved) {
          return;
        }

        this.doctorsResource.reload();
      });
  }
}
