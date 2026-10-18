import { Routes } from '@angular/router';

export const peopleRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./people.component').then(c => c.PeopleComponent)
  }
];
