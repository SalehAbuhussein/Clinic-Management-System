import { isMidnight, lastDayOf } from '@core/utils/day.utils';

import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { formatDate } from '@angular/common';

import { AuthService } from '@core/auth/service/auth.service';

import { Leave } from '@shared/services/leave/leave.types';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButton, MatIcon],
  selector: 'app-leave-card',
  styleUrl: './leave-card.component.scss',
  templateUrl: './leave-card.component.html',
})
export class LeaveCardComponent {
  authService = inject(AuthService);

  item = input.required<Leave>();

  open = output<Leave>();
  approve = output<Leave>();
  reject = output<Leave>();

  /** Two letters for the circle. The full name sits beside it. */
  initials = computed(() => {
    const parts = this.item().fullname.trim().split(/\s+/).filter(Boolean);

    return parts.length ? `${parts[0][0]}${parts.at(-1)![0]}`.toUpperCase() : '?';
  });

  /**
   * Everything the chip needs for the three leave statuses, painted from a
   * Material container pair so the label contrasts with it in both themes.
   */
  status = computed(() => {
    switch (this.item().status) {
      case 1:
        return {
          label: 'Approved',
          icon: 'check_circle',
          container: 'var(--mat-sys-tertiary-container)',
          onContainer: 'var(--mat-sys-on-tertiary-container)',
        };
      case 2:
        return {
          label: 'Rejected',
          icon: 'cancel',
          container: 'var(--mat-sys-error-container)',
          onContainer: 'var(--mat-sys-on-error-container)',
        };
      default:
        return {
          label: 'Pending',
          icon: 'schedule',
          container: 'var(--mat-sys-secondary-container)',
          onContainer: 'var(--mat-sys-on-secondary-container)',
        };
    }
  });

  /** Only an admin decides, and only while the request is still pending. */
  canDecide = computed(() => this.item().status === 0 && this.authService.isAdmin());

  /**
   * When the leave falls, read the way it was asked for.
   *
   * A whole-day leave is stored midnight to midnight with the end exclusive,
   * so the last day shown is the day before `to`. Anything else is part of a
   * single day and reads as clock times, which is the distinction a date-only
   * format would lose.
   */
  when = computed(() => {
    const from = new Date(this.item().from);
    const to = new Date(this.item().to);

    if (!isMidnight(from) || !isMidnight(to)) {
      return `${this.asDate(from)}, ${this.asTime(from)} to ${this.asTime(to)}`;
    }

    const lastDay = lastDayOf(to);

    return this.asDate(from) === this.asDate(lastDay)
      ? this.asDate(from)
      : `${this.asDate(from)} to ${this.asDate(lastDay)}`;
  });

  /**
   * @param date Any moment.
   * @returns Its day, written out.
   */
  asDate = (date: Date): string => formatDate(date, 'd MMM y', 'en-US');

  /**
   * @param date Any moment.
   * @returns Its clock time.
   */
  asTime = (date: Date): string => formatDate(date, 'HH:mm', 'en-US');
}
