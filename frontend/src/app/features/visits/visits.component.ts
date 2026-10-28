import { ActivatedRoute, Router } from '@angular/router';
import { ChangeDetectionStrategy, Component, computed, effect, inject, linkedSignal, signal } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatIcon } from '@angular/material/icon';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { formatDate } from '@angular/common';

import { Observable } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { VisitService } from '@shared/services/visit/visit.service';

import { dateFromUrl, pageFromUrl } from '@core/utils/url-state.utils';
import { rangeBetween, toIsoDate } from '@core/utils/day.utils';

import { ConfirmDialogComponent } from '@shared/components/confirm-dialog/confirm-dialog.component';
import { DateRangePickerComponent } from '@shared/components/date-range-picker/date-range-picker.component';
import { DiagnosisDialogComponent } from './components/diagnosis-dialog/diagnosis-dialog.component';
import { ExportButtonComponent } from '@shared/components/export-button/export-button.component';
import { PaginationComponent } from '@shared/components/pagination/pagination.component';
import { PrescriptionDialogComponent } from './components/prescription-dialog/prescription-dialog.component';
import { VisitDetailDialogComponent } from './components/visit-detail-dialog/visit-detail-dialog.component';
import { VisitEditDialogComponent } from './components/visit-edit-dialog/visit-edit-dialog.component';
import { VisitRowComponent } from './components/visit-row/visit-row.component';

import { ApiError } from '@core/auth/types/auth.types';
import { ConfirmDialogData } from '@shared/components/confirm-dialog/confirm-dialog.types';
import { Visit } from '@shared/services/visit/visit.types';
import { VisitDetailAction } from './types/visits.types';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DateRangePickerComponent, ExportButtonComponent, MatIcon, MatProgressSpinner, PaginationComponent, VisitRowComponent],
  selector: 'app-visits',
  styleUrl: './visits.component.scss',
  templateUrl: './visits.component.html',
})
export class VisitsComponent {
  dialog = inject(MatDialog);
  route = inject(ActivatedRoute);
  router = inject(Router);
  snackbar = inject(MatSnackBar);
  visitService = inject(VisitService);

  from = signal(dateFromUrl(this.route, 'from'));
  to = signal(dateFromUrl(this.route, 'to'));

  rangeKey = computed(() => `${toIsoDate(this.from())}_${toIsoDate(this.to())}`);
  isOneDay = computed(() => toIsoDate(this.from()) === toIsoDate(this.to()));
  rangeLabel = computed(() => this.isOneDay()
    ? formatDate(this.from(), 'EEEE d MMMM', 'en-US')
    : `${formatDate(this.from(), 'd MMM', 'en-US')} to ${formatDate(this.to(), 'd MMM', 'en-US')}`);

  page = linkedSignal<string, number>({
    source: this.rangeKey,
    computation: (_, previous) => previous ? 1 : pageFromUrl(this.route),
  });

  exportFilters = computed(() => rangeBetween(this.from(), this.to()));

  visitsResource = this.visitService.getVisitResource(this.from, this.to, this.page);

  visits = computed(() => this.visitsResource.value().items);
  total = computed(() => this.visitsResource.value().total);
  perPage = computed(() => this.visitsResource.value().perPage || 10);

  constructor() {
    effect(() => {
      this.router.navigate([], {
        queryParams: { from: toIsoDate(this.from()), to: toIsoDate(this.to()), page: this.page() },
        relativeTo: this.route,
        replaceUrl: true,
      });
    });

    this.openVisitFromUrl();
  }

  /**
   * Opens the visit a link named, so another screen can hand one over rather
   * than repeating everything this one does with it.
   *
   * The range narrows to the visit's own day, because a link only carries the
   * id and the list behind the dialog should be showing it.
   *
   * The effect above rewrites the query string from the range and the page
   * alone, so the `visitId` drops off once it has been read.
   */
  openVisitFromUrl(): void {
    const id = Number(this.route.snapshot.queryParamMap.get('visitId'));

    if (!Number.isInteger(id) || id < 1) {
      return;
    }

    this.visitService.getVisit(id).pipe(takeUntilDestroyed()).subscribe({
      next: (visit) => {
        this.from.set(new Date(visit.checkinTime));
        this.to.set(new Date(visit.checkinTime));
        this.openVisit(visit);
      },
      error: (error: ApiError) => this.snackbar.open(error.message, 'Dismiss', { duration: 8000 }),
    });
  }

  /**
   * Show everything on a visit, and act on whatever the dialog closes with.
   *
   * Each action opens another dialog, so the detail closes first rather than
   * stacking two on top of each other.
   *
   * @param visit The row that was picked.
   */
  openVisit = (visit: Visit) => {
    this.dialog.open(VisitDetailDialogComponent, { width: '520px', data: { visit } })
      .afterClosed()
      .subscribe((action: VisitDetailAction | undefined) => {
        switch (action) {
          case 'diagnose':
            this.diagnoseVisit(visit);
            break;
          case 'prescribe':
            this.prescribeVisit(visit);
            break;
          case 'edit':
            this.editVisit(visit);
            break;
          case 'checkout':
            this.checkoutVisit(visit);
            break;
          case 'delete':
            this.deleteVisit(visit);
            break;
        }
      });
  }

  /**
   * Write or correct the visit's diagnosis. Checkout is refused until this
   * exists, so it is the step between arriving and leaving.
   *
   * @param visit The row that was picked.
   */
  diagnoseVisit = (visit: Visit) => {
    this.dialog.open(DiagnosisDialogComponent, { width: '480px', data: visit })
      .afterClosed()
      .subscribe((saved: boolean | undefined) => {
        if (!saved) {
          return;
        }

        this.visitsResource.reload();
      });
  }

  /**
   * Write or replace the visit's prescription. Unlike the diagnosis, checkout
   * does not require one: plenty of visits end without medication.
   *
   * @param visit The row that was picked.
   */
  prescribeVisit = (visit: Visit) => {
    this.dialog.open(PrescriptionDialogComponent, { width: '880px', data: visit })
      .afterClosed()
      .subscribe((saved: boolean | undefined) => {
        if (!saved) {
          return;
        }

        this.visitsResource.reload();
      });
  }

  /**
   * Correct an open visit's arrival time or notes.
   *
   * @param visit The row that was picked.
   */
  editVisit = (visit: Visit) => {
    this.dialog.open(VisitEditDialogComponent, { width: '480px', data: visit })
      .afterClosed()
      .subscribe((saved: boolean | undefined) => {
        if (!saved) {
          return;
        }

        this.visitsResource.reload();
      });
  }

  /**
   * Close a visit. The API refuses without a diagnosis, and only a doctor can
   * write one, so a visit the doctor has not finished answers 422 here.
   *
   * @param visit The row that was picked.
   */
  checkoutVisit = (visit: Visit) => this.confirmThen(
    {
      title: `Check out ${visit.patientName}?`,
      message: 'This closes and locks the visit. Nothing on it can be changed afterwards.',
      confirmLabel: 'Check out',
    },
    this.visitService.checkoutVisit(visit.id),
    'Visit checked out',
  );

  /**
   * Remove a visit that should not have been opened.
   *
   * @param visit The row that was picked.
   */
  deleteVisit = (visit: Visit) => this.confirmThen(
    {
      title: `Delete ${visit.patientName}'s visit?`,
      message: 'The record goes for good. The appointment stays marked attended, so correct that separately if it was a mistake.',
      confirmLabel: 'Delete visit',
      dismissLabel: 'Keep visit',
      destructive: true,
    },
    this.visitService.deleteVisit(visit.id),
    'Visit deleted',
  );

  /**
   * Asks before a change, then runs it and refreshes the list.
   *
   * @param data What the dialog should say.
   * @param request The call to make once confirmed. Cold, so nothing is sent
   * until the subscribe below.
   * @param message What to say when it succeeds.
   */
  confirmThen(data: ConfirmDialogData, request: Observable<void>, message: string): void {
    this.dialog.open(ConfirmDialogComponent, { width: '400px', data })
      .afterClosed()
      .subscribe((confirmed: boolean | undefined) => {
        if (!confirmed) {
          return;
        }

        request.subscribe({
          next: () => {
            this.snackbar.open(message, 'Dismiss', { duration: 4000 });
            this.visitsResource.reload();
          },
          error: (error: ApiError) => this.snackbar.open(error.message, 'Dismiss', { duration: 8000 }),
        });
      });
  }
}
