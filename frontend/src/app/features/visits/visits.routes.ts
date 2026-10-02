import { Routes } from '@angular/router';

export const visitsRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./visits.component').then(c => c.VisitsComponent)
  }
];
