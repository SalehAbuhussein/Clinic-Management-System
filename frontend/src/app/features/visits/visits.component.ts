import { ActivatedRoute, Router } from '@angular/router';
import { Component, computed, effect, inject, linkedSignal, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialog } from '@angular/material/dialog';
import { MatIcon } from '@angular/material/icon';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { formatDate } from '@angular/common';

import { Observable } from 'rxjs';

import { VisitService } from '@shared/services/visit/visit.service';

import { ConfirmDialogComponent } from '@shared/components/confirm-dialog/confirm-dialog.component';
import { DiagnosisDialogComponent } from './components/diagnosis-dialog/diagnosis-dialog.component';
import { PrescriptionDialogComponent } from './components/prescription-dialog/prescription-dialog.component';
import { VisitDetailAction, VisitDetailDialogComponent } from './components/visit-detail-dialog/visit-detail-dialog.component';
import { PaginationComponent } from '@shared/components/pagination/pagination.component';
import { VisitEditDialogComponent } from './components/visit-edit-dialog/visit-edit-dialog.component';
import { VisitRowComponent } from './components/visit-row/visit-row.component';

import { ApiError } from '@core/auth/types/auth.types';
import { ConfirmDialogData } from '@shared/components/confirm-dialog/confirm-dialog.types';
import { Visit } from '@shared/services/visit/visit.types';

@Component({
  imports: [FormsModule, MatButton, MatDatepickerModule, MatIcon, MatIconButton, MatProgressSpinner, PaginationComponent, VisitRowComponent],
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

  day = signal(this.dayFromUrl());

  dayParam = computed(() => formatDate(this.day(), 'yyyy-MM-dd', 'en-US'));
  dayLabel = computed(() => formatDate(this.day(), 'EEEE d MMMM', 'en-US'));
  isToday = computed(() => this.dayParam() === formatDate(new Date(), 'yyyy-MM-dd', 'en-US'));

  // Resets to the first page on a new day, but takes the URL's page on the
  // first run so a reload or a shared link lands where it left off.
  page = linkedSignal<string, number>({
    source: this.dayParam,
    computation: (_, previous) => previous ? 1 : this.pageFromUrl(),
  });

  visitsResource = this.visitService.getVisitResource(this.day, this.page);

  visits = computed(() => this.visitsResource.value().items);
  total = computed(() => this.visitsResource.value().total);
  perPage = computed(() => this.visitsResource.value().perPage || 10);

  constructor() {
    effect(() => {
      this.router.navigate([], {
        queryParams: { day: this.dayParam(), page: this.page() },
        relativeTo: this.route,
        replaceUrl: true,
      });
    });
  }

  /**
   * Reads the day to show. A date with no time parses as UTC, so the midnight
   * is spelled out to keep it local.
   *
   * @returns The day from the URL, or today.
   */
  dayFromUrl(): Date {
    const value = this.route.snapshot.queryParamMap.get('day');
    const day = value ? new Date(`${value}T00:00:00`) : null;

    return day && !Number.isNaN(day.getTime()) ? day : new Date();
  }

  /**
   * Move the shown day.
   *
   * @param days Offset in days; negative goes back.
   */
  shiftDayBy = (days: number) => this.day.update((current) => new Date(current.getFullYear(), current.getMonth(), current.getDate() + days));

  goToToday = () => this.day.set(new Date());

  /**
   * @returns The page from the URL, or the first one.
   */
  pageFromUrl(): number {
    const page = Number(this.route.snapshot.queryParamMap.get('page'));

    return Number.isInteger(page) && page > 0 ? page : 1;
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
    this.dialog.open(VisitDetailDialogComponent, { width: '520px', data: visit })
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
