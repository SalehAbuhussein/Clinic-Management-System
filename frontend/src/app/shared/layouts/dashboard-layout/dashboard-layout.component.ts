import { Component, inject, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatFabButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatListItem, MatListItemIcon, MatListItemTitle, MatNavList } from '@angular/material/list';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { AuthService } from '@core/auth/service/auth.service';

import { BookAppointmentDialogComponent } from '@features/book-appointment-dialog/book-appointment-dialog.component';

@Component({
  imports: [RouterLink, RouterLinkActive, RouterOutlet, MatFabButton, MatIcon, MatNavList, MatListItem, MatListItemIcon, MatListItemTitle],
  selector: 'app-dashboard-layout',
  styleUrl: './dashboard-layout.component.scss',
  templateUrl: './dashboard-layout.component.html',
})
export class DashboardLayoutComponent implements OnInit {
  authService = inject(AuthService);
  dialog = inject(MatDialog);
  router = inject(Router);

  /**
   * Angular onInit lifecycle method
   */
  ngOnInit(): void {
    this.initComponent();
  }

  /**
   * Init component data
   */
  initComponent = () => {
    this.sendDoctorToTheirOwnScreen();
  }

  /**
   * Today is the dashboard's index route and the API serves it to Admin and
   * Staff only, so a doctor landing there meets a 403. Visits is the one screen
   * their role can load, and the API scopes it to their own patients.
   */
  sendDoctorToTheirOwnScreen = (): void => {
    if (this.authService.role() === 'Doctor' && !this.router.url.startsWith('/dashboard/')) {
      this.router.navigate(['/dashboard/visits']);
    }
  }

  /**
   * Logout and Navigate user to login page
   */
  logout = () => {
    this.authService.logout();
    this.router.navigate(['/']);
  }

  /**
   * Open Booking dialog flow
   */
  openBookingDialog = () => {
    this.dialog.open(BookAppointmentDialogComponent, { width: '560px' });
  }
}
