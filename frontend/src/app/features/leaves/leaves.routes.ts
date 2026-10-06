import { Routes } from '@angular/router';

export const leavesRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./leaves.component').then(c => c.LeavesComponent)
  }
];
