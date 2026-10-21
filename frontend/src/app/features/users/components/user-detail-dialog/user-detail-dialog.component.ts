import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogTitle } from '@angular/material/dialog';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';

import { AuthService } from '@core/auth/service/auth.service';

import { User } from '@shared/services/user/user.types';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButton, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogTitle, MatIcon],
  selector: 'app-user-detail-dialog',
  styleUrl: './user-detail-dialog.component.scss',
  templateUrl: './user-detail-dialog.component.html',
})
export class UserDetailDialogComponent {
  authService = inject(AuthService);

  user = inject<User>(MAT_DIALOG_DATA);

  isSameUser = computed(() => this.authService.userId() === this.user.id);
}
