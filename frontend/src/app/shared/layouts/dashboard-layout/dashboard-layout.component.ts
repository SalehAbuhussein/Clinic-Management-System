import { Component, inject } from '@angular/core';
import { MatFabButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatListItem, MatListItemIcon, MatListItemTitle, MatNavList } from '@angular/material/list';
import { Router, RouterOutlet } from '@angular/router';

import { AuthService } from '@core/auth/service/auth.service';

@Component({
  imports: [RouterOutlet, MatFabButton, MatIcon, MatNavList, MatListItem, MatListItemIcon, MatListItemTitle],
  selector: 'app-dashboard-layout',
  styleUrl: './dashboard-layout.component.scss',
  templateUrl: './dashboard-layout.component.html',
})
export class DashboardLayoutComponent {
  router = inject(Router);
  authService = inject(AuthService);

  /**
   * Logout and Navigate user to login page
   */
  logout = () => {
    this.authService.logout();
    this.router.navigate(['/']);
  }
}
