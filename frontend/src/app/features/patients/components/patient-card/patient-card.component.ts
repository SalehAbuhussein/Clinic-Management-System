import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { Patient } from '@shared/services/patient/patient.types';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-patient-card',
  styleUrl: './patient-card.component.scss',
  templateUrl: './patient-card.component.html',
})
export class PatientCardComponent {
  patient = input.required<Patient>();

  open = output<Patient>();

  avatarInitials = computed(() => (this.patient().fullname ?? '')
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((name) => name[0].toUpperCase())
    .join(''));

  emergencyContact = computed(() => {
    const name = this.patient().emergencyContactName;

    return name ? `Emergency: ${name}` : 'No emergency contact';
  });
}
