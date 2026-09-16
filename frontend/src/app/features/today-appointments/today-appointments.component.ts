import { Component, signal } from '@angular/core';

import { AppointmentsListComponent } from './components/appointments-list/appointments-list.component';
import { MOCK_APPOINTMENTS } from './today-appointments.mock';

@Component({
  imports: [AppointmentsListComponent],
  selector: 'app-today-appointments',
  styleUrl: './today-appointments.component.scss',
  templateUrl: './today-appointments.component.html',
})
export class TodayAppointmentsComponent {
  data = signal(MOCK_APPOINTMENTS);
}
