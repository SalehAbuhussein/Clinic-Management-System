import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { MatButton } from '@angular/material/button';
import { MatButtonToggle, MatButtonToggleGroup } from '@angular/material/button-toggle';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialogActions, MatDialogClose, MatDialogContent, MatDialogRef, MatDialogTitle } from '@angular/material/dialog';
import { MatFormField, MatError, MatHint, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { form, FormField, hidden, maxLength, readonly, required, validate } from '@angular/forms/signals';
import { formatDate } from '@angular/common';

import { LeaveService } from '@shared/services/leave/leave.service';

import { atTime, startOfDay } from '@core/utils/day.utils';

import { ApiError } from '@core/auth/types/auth.types';
import { CreateLeaveRequest } from '@shared/services/leave/leave.types';
import { LeaveRequestData } from '@features/leaves/types/leaves.types';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormField,
    MatButton,
    MatButtonToggle,
    MatButtonToggleGroup,
    MatDatepickerModule,
    MatDialogActions,
    MatDialogClose,
    MatDialogContent,
    MatDialogTitle,
    MatError,
    MatFormField,
    MatHint,
    MatInput,
    MatLabel,
    MatProgressSpinner,
  ],
  selector: 'app-request-leave-dialog',
  styleUrl: './request-leave-dialog.component.scss',
  templateUrl: './request-leave-dialog.component.html',
})
export class RequestLeaveDialogComponent {
  dialogRef = inject(MatDialogRef<RequestLeaveDialogComponent>);
  leaveService = inject(LeaveService);
  snackbar = inject(MatSnackBar);

  isSaving = signal(false);
  error = signal('');

  leaveModel = signal<LeaveRequestData>({
    mode: 'day',
    from: null,
    to: null,
    day: null,
    startTime: '09:00',
    endTime: '13:00',
    reason: '',
  });
  leaveForm = form(this.leaveModel, (leave) => {
    readonly(leave.day);
    readonly(leave.from);
    readonly(leave.to);

    hidden(leave.day, { when: ({ valueOf }) => valueOf(leave.mode) !== 'day' });
    hidden(leave.startTime, { when: ({ valueOf }) => valueOf(leave.mode) !== 'day' });
    hidden(leave.endTime, { when: ({ valueOf }) => valueOf(leave.mode) !== 'day' });
    hidden(leave.from, { when: ({ valueOf }) => valueOf(leave.mode) !== 'range' });
    hidden(leave.to, { when: ({ valueOf }) => valueOf(leave.mode) !== 'range' });

    required(leave.day);
    required(leave.startTime);

    required(leave.endTime);
    validate(leave.endTime, ({ valueOf }) => valueOf(leave.endTime) <= valueOf(leave.startTime)
      ? { kind: 'endBeforeStart', message: 'The end time must come after the start time' }
      : null);

    required(leave.from);

    required(leave.to);
    validate(leave.to, ({ valueOf }) => {
      const from = valueOf(leave.from);
      const to = valueOf(leave.to);

      return from && to && to < from
        ? { kind: 'endBeforeStart', message: 'The last day cannot come before the first' }
        : null;
    });

    required(leave.reason);
    maxLength(leave.reason, 255);
  });

  isSingleDay = computed(() => this.leaveModel().mode === 'day');

  dayCount = computed(() => {
    const { from, to } = this.leaveModel();

    if (!from || !to || to < from) {
      return 0;
    }

    return Math.round((startOfDay(to).getTime() - startOfDay(from).getTime()) / 86_400_000) + 1;
  });

  hourCount = computed(() => {
    const { day, startTime, endTime } = this.leaveModel();
    const chosenDay = day ?? new Date();
    const hours = (atTime(chosenDay, endTime).getTime() - atTime(chosenDay, startTime).getTime()) / 3_600_000;

    return hours > 0 ? hours : 0;
  });

  /**
   * Switches which half of the form applies. The other half keeps its values
   * but stops counting, so flipping back and forth loses nothing.
   *
   * @param mode The newly picked mode.
   */
  setMode = (mode: LeaveRequestData['mode']): void => {
    this.leaveModel.update((model) => ({ ...model, mode }));
  }

  /**
   * Sends the request, then closes so the screen can refresh its list.
   */
  requestLeave = (event: Event): void => {
    event.preventDefault();

    if (this.leaveForm().invalid()) {
      this.leaveForm().markAsTouched();
      return;
    }

    if (this.isSaving()) {
      return;
    }

    this.isSaving.set(true);
    this.error.set('');

    this.leaveService.createLeave(this.getCreateLeaveRequest()).subscribe({
      next: this.finish,
      error: this.fail,
    });
  }

  /**
   * The body the API takes. Both modes produce one instant pair, because the
   * API has no notion of a whole-day leave — only of a window.
   *
   * Times carry the local offset rather than `toISOString`, so the
   * datetimeoffset column keeps the wall clock this browser read.
   */
  getCreateLeaveRequest = (): CreateLeaveRequest => {
    const { reason } = this.leaveForm().value();
    const { from, to } = this.isSingleDay() ? this.getDayWindow() : this.getRangeWindow();

    return {
      from: formatDate(from, "yyyy-MM-dd'T'HH:mm:ssZZZZZ", 'en-US'),
      to: formatDate(to, "yyyy-MM-dd'T'HH:mm:ssZZZZZ", 'en-US'),
      reason: reason.trim(),
    };
  }

  /**
   * Part of one day, so the two clock times land on the same date and no day
   * is added. An 09:00 to 13:00 leave leaves the afternoon bookable.
   */
  getDayWindow = (): { from: Date, to: Date } => {
    const { day, startTime, endTime } = this.leaveModel();

    return {
      from: atTime(day!, startTime),
      to: atTime(day!, endTime),
    };
  }

  /**
   * Whole days, so both ends sit on midnight and the end moves to the day
   * after the one picked: the API compares half-open ranges, and a leave
   * ending at the 16th's midnight would leave the 16th itself bookable.
   *
   * Built from date parts rather than by adding 24 hours, so a
   * daylight-saving day stays the right length.
   */
  getRangeWindow = (): { from: Date, to: Date } => {
    const { from, to } = this.leaveModel();

    const start = startOfDay(from!);
    const end = startOfDay(to!);

    end.setDate(end.getDate() + 1);

    return { from: start, to: end };
  }

  /**
   * Confirms, then closes truthy so the caller knows to reload.
   */
  finish = (): void => {
    this.snackbar.open('Leave requested', 'Dismiss', { duration: 5000 });
    this.dialogRef.close(true);
  }

  /**
   * Leaves the form as it was and shows the server's own wording, which here
   * carries the real reasons: an overlapping request, or an admin asking.
   *
   * @param error The mapped failure from the error interceptor.
   */
  fail = (error: ApiError): void => {
    this.isSaving.set(false);
    this.error.set(error.message);
  }
}
