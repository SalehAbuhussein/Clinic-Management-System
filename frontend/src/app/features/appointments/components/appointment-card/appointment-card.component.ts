import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatIcon } from '@angular/material/icon';

import { AppointmentCard, AppointmentStatus } from '@shared/services/appointment/appointment.types';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, MatIcon],
  selector: 'app-appointment-card',
  styleUrl: './appointment-card.component.scss',
  templateUrl: './appointment-card.component.html',
})
export class AppointmentCardComponent {
  readonly Status = AppointmentStatus;

  item = input.required<AppointmentCard>();

  open = output<AppointmentCard>();

  statusChip = computed(() => {
    switch (this.item().status) {
      case AppointmentStatus.Attended:
        return {
          label: 'Attended',
          icon: 'check_circle',
          accent: 'var(--status-attended-accent)',
          container: 'var(--status-attended-container)',
          onContainer: 'var(--status-attended-on)',
        };
      case AppointmentStatus.NoShow:
        return {
          label: 'No-show',
          icon: 'person_off',
          accent: 'var(--status-not-attended-accent)',
          container: 'var(--status-not-attended-container)',
          onContainer: 'var(--status-not-attended-on)',
        };
      case AppointmentStatus.Cancelled:
        return {
          label: 'Cancelled',
          icon: 'cancel',
          accent: 'var(--status-cancelled-accent)',
          container: 'var(--status-cancelled-container)',
          onContainer: 'var(--status-cancelled-on)',
        };
      default:
        return {
          label: 'Scheduled',
          icon: 'schedule',
          accent: 'var(--status-scheduled-accent)',
          container: 'var(--status-scheduled-container)',
          onContainer: 'var(--status-scheduled-on)',
        };
    }
  });
}
