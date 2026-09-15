import { Routes } from "@angular/router";

export const todayAppointmentsRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./today-appointments.component').then(c => c.TodayAppointmentsComponent)
  }
];
