import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogTitle } from '@angular/material/dialog';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';

import { AuthService } from '@core/auth/service/auth.service';

import { Leave, LeaveStatus } from '@shared/services/leave/leave.types';
import { isMidnight, lastDayOf } from '@core/utils/day.utils';

/**
 * Shows one leave request, and offers an admin the decision on a pending one.
 *
 * A whole-day range is stored half-open, ending on the following midnight, so
 * `endsAt` steps back a day before it is shown and the clock times are left
 * out entirely.
 */
@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, MatButton, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogTitle, MatIcon],
  selector: 'app-leave-detail-dialog',
  styleUrl: './leave-detail-dialog.component.scss',
  templateUrl: './leave-detail-dialog.component.html',
})
export class LeaveDetailDialogComponent {
  authService = inject(AuthService);

  leave = inject<Leave>(MAT_DIALOG_DATA);

  readonly coversWholeDays = isMidnight(new Date(this.leave.from)) && isMidnight(new Date(this.leave.to));

  readonly startsAt = new Date(this.leave.from);

  readonly endsAt = this.coversWholeDays ? lastDayOf(new Date(this.leave.to)) : new Date(this.leave.to);

  readonly isPending = this.leave.status === LeaveStatus.Pending;

  readonly statusBadge = this.badgeFor(this.leave.status);

  /**
   * @param status The leave's status as the API stores it.
   * @returns How to label and illustrate it.
   */
  badgeFor(status: LeaveStatus) {
    switch (status) {
      case LeaveStatus.Approved:
        return { label: 'Approved', icon: 'check_circle' };
      case LeaveStatus.Rejected:
        return { label: 'Rejected', icon: 'cancel' };
      default:
        return { label: 'Pending', icon: 'schedule' };
    }
  }
}
