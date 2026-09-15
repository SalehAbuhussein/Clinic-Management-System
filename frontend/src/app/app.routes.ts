import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/login/login.component').then(c => c.LoginComponent),
  },
  {
    path: 'dashboard',
    loadComponent: () => import('./shared/layouts/dashboard-layout/dashboard-layout.component').then(c => c.DashboardLayoutComponent),
    loadChildren: () => import('./features/today-appointments/today-appointments.routes').then(r => r.todayAppointmentsRoutes),
  }
];
