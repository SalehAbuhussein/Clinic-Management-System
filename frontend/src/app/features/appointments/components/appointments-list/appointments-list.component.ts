import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { AppointmentCardComponent } from '@shared/components/appointment-card/appointment-card.component';

import { AppointmentCard } from '@shared/services/appointment/appointment.types';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AppointmentCardComponent],
  selector: 'app-appointments-list',
  styleUrl: './appointments-list.component.scss',
  templateUrl: './appointments-list.component.html',
})
export class AppointmentsListComponent {
  data = input.required<AppointmentCard[]>();

  open = output<AppointmentCard>();
}
