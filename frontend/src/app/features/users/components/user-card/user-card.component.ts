import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MatIcon } from '@angular/material/icon';

import { AvatarComponent } from '@shared/components/avatar/avatar.component';

import { User } from '@shared/services/user/user.types';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AvatarComponent, MatIcon],
  selector: 'app-user-card',
  styleUrl: './user-card.component.scss',
  templateUrl: './user-card.component.html',
})
export class UserCardComponent {
  user = input.required<User>();

  open = output<User>();
}
