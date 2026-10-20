import { Component, computed, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogTitle } from '@angular/material/dialog';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';

import { AuthService } from '@core/auth/service/auth.service';

import { AppointmentCard } from '@shared/services/appointment/appointment.types';

@Component({
  imports: [DatePipe, MatButton, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogTitle, MatIcon],
  selector: 'app-appointment-detail-dialog',
  styleUrl: './appointment-detail-dialog.component.scss',
  templateUrl: './appointment-detail-dialog.component.html',
})
export class AppointmentDetailDialogComponent {
  authService = inject(AuthService);

  appointment = inject<AppointmentCard>(MAT_DIALOG_DATA);

  /** Only a scheduled appointment can still be checked in, moved or called off. */
  isScheduled = computed(() => this.appointment.status === 0);

  /**
   * The wording and icon for the four appointment statuses. `default` covers
   * 0 Scheduled and anything the API adds later.
   */
  status = computed(() => {
    switch (this.appointment.status) {
      case 1:
        return { label: 'Attended', icon: 'check_circle' };
      case 2:
        return { label: 'No-show', icon: 'person_off' };
      case 3:
        return { label: 'Cancelled', icon: 'cancel' };
      default:
        return { label: 'Scheduled', icon: 'schedule' };
    }
  });
}
