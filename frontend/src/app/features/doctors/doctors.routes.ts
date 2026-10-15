import { Routes } from '@angular/router';

export const doctorsRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./doctors.component').then(c => c.DoctorsComponent)
  }
];
