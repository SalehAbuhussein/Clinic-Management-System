import { Component, computed, input, output, viewChild } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatIcon } from '@angular/material/icon';
import { MatIconButton } from '@angular/material/button';
import { MatMenu, MatMenuItem, MatMenuTrigger } from '@angular/material/menu';

import { AppointmentCard } from '@shared/services/appointment/appointment.types';

@Component({
  imports: [DatePipe, MatIcon, MatIconButton, MatMenu, MatMenuItem, MatMenuTrigger],
  selector: 'app-appointment-card',
  styleUrl: './appointment-card.component.scss',
  templateUrl: './appointment-card.component.html',
})
export class AppointmentCardComponent {
  item = input.required<AppointmentCard>();

  checkIn = output<number>();
  noShow = output<number>();
  cancel = output<number>();
  reschedule = output<AppointmentCard>();

  /** Only a scheduled appointment can still be attended, missed or called off. */
  isScheduled = computed(() => this.item().status === 0);

  hasActions = computed(() => this.isScheduled());

  menuTrigger = viewChild(MatMenuTrigger);

  /**
   * Opens the menu from anywhere on the card. It still anchors to the icon
   * button, so the menu appears in the same place however it was opened.
   */
  openMenu = () => this.menuTrigger()?.openMenu();

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
