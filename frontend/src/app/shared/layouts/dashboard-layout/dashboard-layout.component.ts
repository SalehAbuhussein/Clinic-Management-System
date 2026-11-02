import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatFabButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatListItem, MatListItemIcon, MatListItemTitle, MatNavList } from '@angular/material/list';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ActivatedRoute, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { AuthService } from '@core/auth/service/auth.service';

import { AvatarComponent } from '@shared/components/avatar/avatar.component';
import { ProfileService } from '@shared/services/profile/profile.service';

import { BookAppointmentDialogComponent } from '@features/book-appointment/components/book-appointment-dialog/book-appointment-dialog.component';
import { ProfileDialogComponent } from '@features/profile/components/profile-dialog/profile-dialog.component';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AvatarComponent, RouterLink, RouterLinkActive, RouterOutlet, MatFabButton, MatIcon, MatNavList, MatListItem, MatListItemIcon, MatListItemTitle],
  selector: 'app-dashboard-layout',
  styleUrl: './dashboard-layout.component.scss',
  templateUrl: './dashboard-layout.component.html',
})
export class DashboardLayoutComponent {
  authService = inject(AuthService);
  dialog = inject(MatDialog);
  profileService = inject(ProfileService);
  router = inject(Router);
  route = inject(ActivatedRoute);
  snackbar = inject(MatSnackBar);

  profile = this.profileService.getProfileResource(this.authService.userId);

  profileName = computed(() => {
    const profile = this.profile.value();

    return profile ? `${profile.firstname} ${profile.lastname}` : '';
  });

  constructor() {
    const denied = this.route.snapshot.queryParamMap.get('denied');

    if (denied) {
      this.snackbar.open(`Your role cannot open ${denied}.`, 'Dismiss', { duration: 6000 });
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
   * Opens the booking dialog. Any list showing appointments watches
   * AppointmentService.lastBookedId, so it refreshes itself from here.
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
    }).afterClosed().subscribe(() => this.profile.reload());
  }
}
