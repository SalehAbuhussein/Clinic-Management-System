import { Routes } from '@angular/router';

export const patientsRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./patients.component').then(c => c.PatientsComponent)
  }
];
