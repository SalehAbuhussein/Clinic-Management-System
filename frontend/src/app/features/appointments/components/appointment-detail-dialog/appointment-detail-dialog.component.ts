import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogTitle } from '@angular/material/dialog';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';

import { AuthService } from '@core/auth/service/auth.service';

import { AppointmentCard, AppointmentStatus } from '@shared/services/appointment/appointment.types';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, MatButton, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogTitle, MatIcon],
  selector: 'app-appointment-detail-dialog',
  styleUrl: './appointment-detail-dialog.component.scss',
  templateUrl: './appointment-detail-dialog.component.html',
})
export class AppointmentDetailDialogComponent {
  authService = inject(AuthService);

  appointment = inject<AppointmentCard>(MAT_DIALOG_DATA);

  isScheduled = computed(() => this.appointment.status === AppointmentStatus.Scheduled);

  statusChip = computed(() => {
    switch (this.appointment.status) {
      case AppointmentStatus.Attended:
        return { label: 'Attended', icon: 'check_circle' };
      case AppointmentStatus.NoShow:
        return { label: 'No-show', icon: 'person_off' };
      case AppointmentStatus.Cancelled:
        return { label: 'Cancelled', icon: 'cancel' };
      default:
        return { label: 'Scheduled', icon: 'schedule' };
    }
  });
}
