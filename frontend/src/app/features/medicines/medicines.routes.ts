import { Routes } from '@angular/router';

export const medicinesRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./medicines.component').then(c => c.MedicinesComponent)
  }
];
