import { Component, input } from '@angular/core';

import { AppointmentCardComponent } from '@shared/components/appointment-card/appointment-card.component';

import { AppointmentCard } from '@shared/components/appointment-card/appointment-card.types';

@Component({
  imports: [AppointmentCardComponent],
  selector: 'app-appointments-list',
  styleUrl: './appointments-list.component.scss',
  templateUrl: './appointments-list.component.html',
})
export class AppointmentsListComponent {
  listItems = input.required<AppointmentCard[]>();
}
