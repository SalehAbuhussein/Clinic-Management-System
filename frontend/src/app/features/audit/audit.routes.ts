import { Routes } from '@angular/router';

export const auditRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./audit.component').then(c => c.AuditComponent)
  }
];
