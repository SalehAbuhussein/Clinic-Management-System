import { Component, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogTitle } from '@angular/material/dialog';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';

import { AuthService } from '@core/auth/service/auth.service';

import { Leave } from '@shared/services/leave/leave.types';
import { isMidnight, lastDayOf } from '@core/utils/day.utils';

@Component({
  imports: [DatePipe, MatButton, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogTitle, MatIcon],
  selector: 'app-leave-detail-dialog',
  styleUrl: './leave-detail-dialog.component.scss',
  templateUrl: './leave-detail-dialog.component.html',
})
export class LeaveDetailDialogComponent {
  authService = inject(AuthService);

  leave = inject<Leave>(MAT_DIALOG_DATA);

  readonly wholeDays = isMidnight(new Date(this.leave.from)) && isMidnight(new Date(this.leave.to));

  readonly from = new Date(this.leave.from);

  /**
   * The stored end is midnight of the day after, so a whole-day leave has to
   * step back one day to name the last day it actually covers. A part-day
   * leave keeps its real end, which is a time on the same day.
   */
  readonly to = this.wholeDays ? lastDayOf(new Date(this.leave.to)) : new Date(this.leave.to);

  readonly isPending = this.leave.status === 0;

  readonly status = this.statusOf(this.leave.status);

  /**
   * @param status The leave's status byte.
   * @returns How to label and illustrate it.
   */
  statusOf(status: number) {
    switch (status) {
      case 1:
        return { label: 'Approved', icon: 'check_circle' };
      case 2:
        return { label: 'Rejected', icon: 'cancel' };
      default:
        return { label: 'Pending', icon: 'schedule' };
    }
  }
}
