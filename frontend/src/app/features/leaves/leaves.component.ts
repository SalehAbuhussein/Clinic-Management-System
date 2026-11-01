import { ChangeDetectionStrategy, Component, computed, inject, linkedSignal, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButton } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIcon } from '@angular/material/icon';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';

import { Observable } from 'rxjs';

import { AuthService } from '@core/auth/service/auth.service';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';

import { filtersFromUrl, pageFromUrl, syncUrl } from '@core/utils/url-state.utils';
import { LeaveService } from '@shared/services/leave/leave.service';

import { ConfirmDialogComponent } from '@shared/components/confirm-dialog/confirm-dialog.component';
import { LeaveDetailDialogComponent } from './components/leave-detail-dialog/leave-detail-dialog.component';
import { LeaveFiltersComponent } from './components/leave-filters/leave-filters.component';
import { LeavesListComponent } from './components/leaves-list/leaves-list.component';
import { ExportButtonComponent } from '@shared/components/export-button/export-button.component';
import { PaginationComponent } from '@shared/components/pagination/pagination.component';
import { RequestLeaveDialogComponent } from './components/request-leave-dialog/request-leave-dialog.component';

import { ApiError } from '@core/auth/types/auth.types';
import { ConfirmDialogData } from '@shared/components/confirm-dialog/confirm-dialog.types';
import { Leave, LeaveFilters } from '@shared/services/leave/leave.types';
import { LeaveDetailAction } from './types/leaves.types';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LeaveFiltersComponent,
    ExportButtonComponent,
    LeavesListComponent,
    MatButton,
    MatIcon,
    MatProgressSpinner,
    PaginationComponent,
  ],
  selector: 'app-leaves',
  styleUrl: './leaves.component.scss',
  templateUrl: './leaves.component.html',
})
export class LeavesComponent {
  authService = inject(AuthService);
  dialog = inject(MatDialog);
  leaveService = inject(LeaveService);
  route = inject(ActivatedRoute);
  router = inject(Router);
  snackbar = inject(MatSnackBar);

  filters = signal<LeaveFilters>(filtersFromUrl(this.route));

  debouncedFilters = toSignal(
    toObservable(this.filters).pipe(
      debounceTime(300),
      distinctUntilChanged((one, other) => JSON.stringify(one) === JSON.stringify(other)),
    ),
    { initialValue: this.filters() },
  );

  page = linkedSignal<LeaveFilters, number>({
    source: this.debouncedFilters,
    computation: (_, previous) => previous ? 1 : pageFromUrl(this.route),
  });

  leavesResource = this.leaveService.getLeavesResource(() => ({ ...this.debouncedFilters(), page: this.page() }));

  constructor() {
    syncUrl(this.router, this.route, () => ({ ...this.filters(), page: this.page() }));
  }

  /**
   * Show everything on a leave, and act on whatever the dialog closes with.
   *
   * Each action asks for confirmation, so the detail closes first rather than
   * stacking two dialogs on top of each other.
   *
   * @param leave The row that was picked.
   */
  openLeaveDetailsDialog = (leave: Leave) => {
    this.dialog.open(LeaveDetailDialogComponent, { width: '560px', data: leave })
      .afterClosed()
      .subscribe((action: LeaveDetailAction | undefined) => {
        switch (action) {
          case 'approve':
            this.approveLeave(leave);
            break;
          case 'reject':
            this.rejectLeave(leave);
            break;
        }
      });
  }

  /**
   * Ask for days off. Admins never see this, because the API answers 422 for
   * a leave of their own.
   */
  openRequestLeaveDialog = () => {
    this.dialog.open(RequestLeaveDialogComponent, { width: '560px' })
      .afterClosed()
      .subscribe((requested: boolean | undefined) => {
        if (!requested) {
          return;
        }

        this.leavesResource.reload();
      });
  }

  /**
   * Grant a request. The doctor is then unbookable for those days.
   *
   * @param leave The request being decided.
   */
  approveLeave = (leave: Leave) => {
    return this.decide(
      {
        title: 'Approve this leave?',
        message: `${leave.fullname} cannot be booked for those days once this is approved, and the request can no longer be edited.`,
        confirmLabel: 'Approve',
      },
      this.leaveService.approveLeave(leave.id),
      'Leave approved',
    );
  }

  /**
   * Turn a request down. Those days stay bookable, and the requester may file
   * a new one covering them.
   *
   * @param leave The request being decided.
   */
  rejectLeave = (leave: Leave) => {
    return this.decide(
      {
        title: 'Reject this leave?',
        message: `${leave.fullname} stays bookable for those days. They can request them again.`,
        confirmLabel: 'Reject',
      },
      this.leaveService.rejectLeave(leave.id),
      'Leave rejected',
    );
  }

  /**
   * Asks, runs the decision, then refreshes the list.
   *
   * Both decisions are one way: the API freezes a leave once it is no longer
   * pending, so a mis-click cannot be undone through the app.
   *
   * @param data What the confirmation should say.
   * @param request The call to make once confirmed. Cold, so nothing is sent
   * until the subscribe below.
   * @param message What to say when it succeeds.
   */
  decide(data: ConfirmDialogData, request: Observable<unknown>, message: string): void {
    this.dialog.open(ConfirmDialogComponent, { width: '400px', data })
      .afterClosed()
      .subscribe((confirmed: boolean | undefined) => {
        if (!confirmed) {
          return;
        }

        request.subscribe({
          next: () => {
            this.snackbar.open(message, 'Dismiss', { duration: 4000 });
            this.leavesResource.reload();
          },
          error: (error: ApiError) => this.snackbar.open(error.message, 'Dismiss', { duration: 8000 }),
        });
      });
  }
}
