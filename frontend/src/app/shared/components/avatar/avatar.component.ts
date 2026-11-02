import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';

import { ProfileService } from '@shared/services/profile/profile.service';

/**
 * The round picture that stands for a person, with their initials underneath
 * it. Most accounts have never uploaded anything, so the initials are the
 * normal case rather than a fallback for errors.
 *
 * The size comes from the --avatar-size custom property, so each list sets it
 * in its own stylesheet instead of passing a number through an input.
 */
@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-avatar',
  styleUrl: './avatar.component.scss',
  templateUrl: './avatar.component.html',
})
export class AvatarComponent {
  profileService = inject(ProfileService);

  fileName = input<string | null>(null);
  name = input('');

  imageUrl = computed(() => this.profileService.imageUrlOf(this.fileName()));

  initials = computed(() => (this.name() ?? '')
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0].toUpperCase())
    .join(''));
}
