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
        loadChildren: () => import('./features/today-appointments/today-appointments.routes').then(r => r.todayAppointmentsRoutes),
      },
      {
        path: 'patients',
        canActivate: [roleGuard],
        data: { roles: ['Admin', 'Staff'] },
        loadChildren: () => import('./features/patients/patients.routes').then(r => r.patientsRoutes),
      },
      {
        path: 'visits',
        canActivate: [roleGuard],
        data: { roles: ['Admin', 'Staff', 'Doctor'] },
        loadChildren: () => import('./features/visits/visits.routes').then(r => r.visitsRoutes),
      },
      {
        path: 'stats',
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
        loadChildren: () => import('./features/stats/stats.routes').then(r => r.statsRoutes),
      },
      {
        path: 'users',
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
        loadChildren: () => import('./features/users/users.routes').then(r => r.usersRoutes),
      },
      {
        path: 'people',
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
        loadChildren: () => import('./features/people/people.routes').then(r => r.peopleRoutes),
      },
      {
        path: 'medicines',
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
        loadChildren: () => import('./features/medicines/medicines.routes').then(r => r.medicinesRoutes),
      },
      {
        path: 'doctors',
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
        loadChildren: () => import('./features/doctors/doctors.routes').then(r => r.doctorsRoutes),
      },
      {
        path: 'leaves',
        canActivate: [roleGuard],
        data: { roles: ['Admin', 'Staff', 'Doctor'] },
        loadChildren: () => import('./features/leaves/leaves.routes').then(r => r.leavesRoutes),
      },
      {
        path: 'audit',
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
        loadChildren: () => import('./features/audit/audit.routes').then(r => r.auditRoutes),
      },
    ],
  }
];
