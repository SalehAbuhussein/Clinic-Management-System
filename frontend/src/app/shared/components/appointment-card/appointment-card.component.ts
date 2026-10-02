import { Component, computed, input, output } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatIcon } from '@angular/material/icon';

import { AppointmentCard } from '@shared/services/appointment/appointment.types';

@Component({
  imports: [DatePipe, MatIcon],
  selector: 'app-appointment-card',
  styleUrl: './appointment-card.component.scss',
  templateUrl: './appointment-card.component.html',
})
export class AppointmentCardComponent {
  item = input.required<AppointmentCard>();

  open = output<AppointmentCard>();

  /**
   * Everything the chip needs for the four appointment statuses: its wording,
   * its icon, and the container pair it paints with.
   *
   * The two colours always come from the same role pair, so the label is
   * guaranteed to contrast with its own background in both themes.
   * `default` covers 0 Scheduled and anything the API adds later.
   */
  status = computed(() => {
    switch (this.item().status) {
      case 1:
        return {
          label: 'Attended',
          icon: 'check_circle',
          accent: 'var(--status-attended-accent)',
          container: 'var(--status-attended-container)',
          onContainer: 'var(--status-attended-on)',
        };
      case 2:
        return {
          label: 'No-show',
          icon: 'person_off',
          accent: 'var(--status-not-attended-accent)',
          container: 'var(--status-not-attended-container)',
          onContainer: 'var(--status-not-attended-on)',
        };
      case 3:
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
