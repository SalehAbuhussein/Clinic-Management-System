import { Routes } from "@angular/router";

export const todayScheduleRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./today-schedule.component').then(c => c.TodayScheduleComponent)
  }
];
