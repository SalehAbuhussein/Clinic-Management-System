import { Component, computed, input, output } from '@angular/core';
import { MatIcon } from '@angular/material/icon';

import { User } from '@shared/services/user/user.types';

@Component({
  imports: [MatIcon],
  selector: 'app-user-row',
  styleUrl: './user-row.component.scss',
  templateUrl: './user-row.component.html',
})
export class UserRowComponent {
  user = input.required<User>();

  open = output<User>();

  /**
   * The first letter of the first two names, which is all the avatar shows.
   * Empty when the API sent no name, so the circle stays blank rather than
   * printing something misleading.
   */
  initials = computed(() => (this.user().fullname ?? '')
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0].toUpperCase())
    .join(''));
}
