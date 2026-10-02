import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/login/login.component').then(c => c.LoginComponent),
  },
  {
    path: 'dashboard',
    loadComponent: () => import('./shared/layouts/dashboard-layout/dashboard-layout.component').then(c => c.DashboardLayoutComponent),
    children: [
      {
        path: '',
        loadChildren: () => import('./features/today-appointments/today-appointments.routes').then(r => r.todayAppointmentsRoutes),
      },
      {
        path: 'patients',
        loadChildren: () => import('./features/patients/patients.routes').then(r => r.patientsRoutes),
      },
      {
        path: 'visits',
        loadChildren: () => import('./features/visits/visits.routes').then(r => r.visitsRoutes),
      },
    ],
  }
];
