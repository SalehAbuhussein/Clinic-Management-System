import { Component, computed, input, output, viewChild } from '@angular/core';
import { MatIcon } from '@angular/material/icon';
import { MatIconButton } from '@angular/material/button';
import { MatMenu, MatMenuItem, MatMenuTrigger } from '@angular/material/menu';

import { Patient } from '@shared/services/patient/patient.types';

@Component({
  imports: [MatIcon, MatIconButton, MatMenu, MatMenuItem, MatMenuTrigger],
  selector: 'app-patient-row',
  styleUrl: './patient-row.component.scss',
  templateUrl: './patient-row.component.html',
})
export class PatientRowComponent {
  patient = input.required<Patient>();

  edit = output<Patient>();
  remove = output<Patient>();

  menuTrigger = viewChild(MatMenuTrigger);

  /** Opens the menu from anywhere on the row, anchored to the icon button. */
  openMenu = () => this.menuTrigger()?.openMenu();

  /**
   * The first letter of the first two names, which is all the avatar shows.
   * Empty when the API sent no name, so the circle stays blank rather than
   * printing something misleading.
   */
  initials = computed(() => (this.patient().fullname ?? '')
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0].toUpperCase())
    .join(''));

  /** The emergency contact, or a note that there isn't one. */
  emergencyContact = computed(() => {
    const name = this.patient().emergencyContactName;

    return name ? `Emergency: ${name}` : 'No emergency contact';
  });
}
