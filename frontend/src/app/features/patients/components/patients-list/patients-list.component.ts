import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { Patient } from '@shared/services/patient/patient.types';

import { PatientRowComponent } from '../patient-row/patient-row.component';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PatientRowComponent],
  selector: 'app-patients-list',
  styleUrl: './patients-list.component.scss',
  templateUrl: './patients-list.component.html',
})
export class PatientsListComponent {
  data = input.required<Patient[]>();

  open = output<Patient>();
}
