import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { MatIcon } from '@angular/material/icon';

import { User } from '@shared/services/user/user.types';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatIcon],
  selector: 'app-user-card',
  styleUrl: './user-card.component.scss',
  templateUrl: './user-card.component.html',
})
export class UserCardComponent {
  user = input.required<User>();

  open = output<User>();

  avatarInitials = computed(() => (this.user().fullname ?? '')
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0].toUpperCase())
    .join(''));
}
