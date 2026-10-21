import { Routes } from '@angular/router';

import { authGuard, roleGuard } from '@core/auth/guard/auth.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/login/login.component').then(c => c.LoginComponent),
  },
  {
    path: 'dashboard',
    canActivate: [authGuard],
    loadComponent: () => import('./shared/layouts/dashboard-layout/dashboard-layout.component').then(c => c.DashboardLayoutComponent),
    children: [
      {
        path: '',
        canActivate: [roleGuard],
        data: { roles: ['Admin', 'Staff'] },
        loadComponent: () => import('./features/appointments/appointments.component').then(c => c.AppointmentsComponent),
      },
      {
        path: 'patients',
        canActivate: [roleGuard],
        data: { roles: ['Admin', 'Staff'] },
        loadComponent: () => import('./features/patients/patients.component').then(c => c.PatientsComponent),
      },
      {
        path: 'visits',
        canActivate: [roleGuard],
        data: { roles: ['Admin', 'Staff', 'Doctor'] },
        loadComponent: () => import('./features/visits/visits.component').then(c => c.VisitsComponent),
      },
      {
        path: 'stats',
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
        loadComponent: () => import('./features/stats/stats.component').then(c => c.StatsComponent),
      },
      {
        path: 'users',
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
        loadComponent: () => import('./features/users/users.component').then(c => c.UsersComponent),
      },
      {
        path: 'people',
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
        loadComponent: () => import('./features/people/people.component').then(c => c.PeopleComponent),
      },
      {
        path: 'medicines',
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
        loadComponent: () => import('./features/medicines/medicines.component').then(c => c.MedicinesComponent),
      },
      {
        path: 'doctors',
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
        loadComponent: () => import('./features/doctors/doctors.component').then(c => c.DoctorsComponent),
      },
      {
        path: 'leaves',
        canActivate: [roleGuard],
        data: { roles: ['Admin', 'Staff', 'Doctor'] },
        loadComponent: () => import('./features/leaves/leaves.component').then(c => c.LeavesComponent),
      },
      {
        path: 'audit',
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
        loadComponent: () => import('./features/audit/audit.component').then(c => c.AuditComponent),
      },
    ],
  },
  {
    path: '**',
    redirectTo: '',
  }
];
