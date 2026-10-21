import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { Patient } from '@shared/services/patient/patient.types';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-patient-row',
  styleUrl: './patient-row.component.scss',
  templateUrl: './patient-row.component.html',
})
export class PatientRowComponent {
  patient = input.required<Patient>();

  open = output<Patient>();

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
