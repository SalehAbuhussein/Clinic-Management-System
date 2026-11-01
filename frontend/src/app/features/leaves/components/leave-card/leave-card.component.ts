import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';

import { AuthService } from '@core/auth/service/auth.service';

import { Leave, LeaveStatus } from '@shared/services/leave/leave.types';

import { formatDay, formatTime, isMidnight, lastDayOf } from '@core/utils/day.utils';

/**
 * One leave request in the list.
 *
 * A whole-day leave is stored midnight to midnight with the end exclusive, so
 * the last day shown is the day before `to`. Anything else is part of a single
 * day and reads as clock times, which a date-only format would lose.
 */
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

  avatarInitials = computed(() => this.item().fullname
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((name) => name[0].toUpperCase())
    .join(''));

  statusChip = computed(() => {
    switch (this.item().status) {
      case LeaveStatus.Approved:
        return {
          label: 'Approved',
          icon: 'check_circle',
          container: 'var(--mat-sys-tertiary-container)',
          onContainer: 'var(--mat-sys-on-tertiary-container)',
        };
      case LeaveStatus.Rejected:
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

  canDecide = computed(() => this.item().status === LeaveStatus.Pending && this.authService.isAdmin());

  periodLabel = computed(() => {
    const from = new Date(this.item().from);
    const to = new Date(this.item().to);

    if (!isMidnight(from) || !isMidnight(to)) {
      return `${formatDay(from)}, ${formatTime(from)} to ${formatTime(to)}`;
    }

    const lastDay = lastDayOf(to);

    return formatDay(from) === formatDay(lastDay)
      ? formatDay(from)
      : `${formatDay(from)} to ${formatDay(lastDay)}`;
  });
}
