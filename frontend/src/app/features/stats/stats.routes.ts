import { Routes } from '@angular/router';

export const statsRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./stats.component').then(c => c.StatsComponent)
  }
];
