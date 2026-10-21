import { ChangeDetectionStrategy, Component, computed, inject, linkedSignal, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatButton } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormField, MatLabel, MatPrefix } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatInput } from '@angular/material/input';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';

import { debounceTime, distinctUntilChanged } from 'rxjs';

import { pageFromUrl, syncUrl } from '@core/utils/url-state.utils';
import { MedicineService } from '@shared/services/medicine/medicine.service';

import { ConfirmDialogComponent } from '@shared/components/confirm-dialog/confirm-dialog.component';
import { MedicineDetailDialogComponent } from './components/medicine-detail-dialog/medicine-detail-dialog.component';
import { MedicineFormDialogComponent } from './components/medicine-form-dialog/medicine-form-dialog.component';
import { PaginationComponent } from '@shared/components/pagination/pagination.component';

import { ApiError } from '@core/auth/types/auth.types';
import { Medicine } from '@shared/services/medicine/medicine.types';
import { MedicineDetailAction } from './types/medicines.types';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, MatButton, MatFormField, MatIcon, MatInput, MatLabel, MatPrefix, MatProgressSpinner, PaginationComponent],
  selector: 'app-medicines',
  styleUrl: './medicines.component.scss',
  templateUrl: './medicines.component.html',
})
export class MedicinesComponent {
  dialog = inject(MatDialog);
  medicineService = inject(MedicineService);
  route = inject(ActivatedRoute);
  router = inject(Router);
  snackbar = inject(MatSnackBar);

  search = signal(this.route.snapshot.queryParamMap.get('name') ?? '');

  debouncedSearch = toSignal(
    toObservable(this.search).pipe(debounceTime(300), distinctUntilChanged()),
    { initialValue: this.search() },
  );

  // Back to the first page on a new search, but the URL's page on the first
  // run, so a reload or a shared link lands where it left off.
  page = linkedSignal<string, number>({
    source: this.debouncedSearch,
    computation: (_, previous) => previous ? 1 : pageFromUrl(this.route),
  });

  medicinesResource = this.medicineService.getMedicineResource(() => ({
    name: this.debouncedSearch().trim() || undefined,
    page: this.page(),
  }));

  medicines = computed(() => this.medicinesResource.value().items);
  total = computed(() => this.medicinesResource.value().total);
  perPage = computed(() => this.medicinesResource.value().perPage || 10);

  constructor() {
    syncUrl(this.router, this.route, () => ({ name: this.debouncedSearch().trim(), page: this.page() }));
  }

  /**
   * Remove a medicine. The API refuses one that any prescription names, and
   * says so.
   *
   * @param medicine The row that was picked.
   */
  deleteMedicine = (medicine: Medicine) => {
    this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        title: 'Delete this medicine?',
        message: `${medicine.genericname} can no longer be prescribed. Prescriptions that already name it keep it.`,
        confirmLabel: 'Delete',
      },
    })
      .afterClosed()
      .subscribe((confirmed: boolean | undefined) => {
        if (!confirmed) {
          return;
        }

        this.medicineService.deleteMedicine(medicine.id).subscribe({
          next: () => {
            this.snackbar.open('Medicine deleted', 'Dismiss', { duration: 4000 });
            this.medicinesResource.reload();
          },
          error: (error: ApiError) => this.snackbar.open(error.message, 'Dismiss', { duration: 8000 }),
        });
      });
  }

  /**
   * Opens the form in whichever mode the caller wants and refreshes on a save.
   *
   * @param medicine The row being edited, or null to add one.
   */
  openDetail(medicine: Medicine): void {
    this.dialog.open(MedicineDetailDialogComponent, { width: '420px', data: medicine })
      .afterClosed()
      .subscribe((action: MedicineDetailAction | undefined) => {
        if (action === 'edit') {
          this.openForm(medicine);
        }
      });
  }

  openForm(medicine: Medicine | null): void {
    this.dialog.open(MedicineFormDialogComponent, { width: '560px', maxWidth: '100vw', data: medicine })
      .afterClosed()
      .subscribe((saved: boolean | undefined) => {
        if (!saved) {
          return;
        }

        this.medicinesResource.reload();
      });
  }
}
