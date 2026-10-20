import { Routes } from "@angular/router";

export const appointmentsRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./appointments.component').then(c => c.AppointmentsComponent)
  }
];
