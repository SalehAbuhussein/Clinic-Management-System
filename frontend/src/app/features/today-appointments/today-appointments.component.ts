import { Component, inject, OnInit, signal } from '@angular/core';

import { AppointmentService } from '@shared/services/appointment/appointment.service';

import { AppointmentsListComponent } from './components/appointments-list/appointments-list.component';

import { AppointmentCard } from '@shared/services/appointment/appointment.types';

@Component({
  imports: [AppointmentsListComponent],
  selector: 'app-today-appointments',
  styleUrl: './today-appointments.component.scss',
  templateUrl: './today-appointments.component.html',
})
export class TodayAppointmentsComponent implements OnInit {
  appointmentService = inject(AppointmentService);

  data = signal<AppointmentCard[]>([]);

  /**
   * Angular onInit lifecycle method
   */
  ngOnInit(): void {
    this.initComponent();
  }

  /**
   * Initialize Component data
   */
  initComponent = () => {
    this.getAppointmentsList();
  }

  /**
   * Get Appointment List from API
   */
  getAppointmentsList = () => {
    this.appointmentService.getAppointmentList().subscribe({
      next: items => this.data.set(items),
      error: err => console.error(err),
    });
  }
}
