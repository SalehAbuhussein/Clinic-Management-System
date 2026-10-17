import { Component, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatFabButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatListItem, MatListItemIcon, MatListItemTitle, MatNavList } from '@angular/material/list';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { AuthService } from '@core/auth/service/auth.service';

import { BookAppointmentDialogComponent } from '@features/book-appointment/components/book-appointment-dialog/book-appointment-dialog.component';
import { ProfileDialogComponent } from '@features/profile/components/profile-dialog/profile-dialog.component';

@Component({
  imports: [RouterLink, RouterLinkActive, RouterOutlet, MatFabButton, MatIcon, MatNavList, MatListItem, MatListItemIcon, MatListItemTitle],
  selector: 'app-dashboard-layout',
  styleUrl: './dashboard-layout.component.scss',
  templateUrl: './dashboard-layout.component.html',
})
export class DashboardLayoutComponent {
  authService = inject(AuthService);
  dialog = inject(MatDialog);
  router = inject(Router);

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

  /**
   * Open Profile dialog
   */
  openProfileDialog = (event: Event) => {
    event.preventDefault();

    this.dialog.open(ProfileDialogComponent, {
      width: '800px',
      maxWidth: '100vw'
    });
  }
}
