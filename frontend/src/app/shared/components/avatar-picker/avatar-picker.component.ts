import { ChangeDetectionStrategy, Component, computed, input, OnDestroy, signal } from '@angular/core';
import { MatButton } from '@angular/material/button';

import { AvatarComponent } from '@shared/components/avatar/avatar.component';

/**
 * The avatar with the controls to replace or clear it.
 *
 * Nothing reaches the server here. The picker only records what the form
 * should do on save, so trying five pictures still uploads one, and closing
 * the dialog leaves the stored file alone. The form reads `picked` and
 * `isCleared` when it submits.
 */
@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AvatarComponent, MatButton],
  selector: 'app-avatar-picker',
  styleUrl: './avatar-picker.component.scss',
  templateUrl: './avatar-picker.component.html',
})
export class AvatarPickerComponent implements OnDestroy {
  fileName = input<string | null>(null);
  name = input('');
  disabled = input(false);

  maxBytes = 500 * 1024; // 500KB

  picked = signal<File | null>(null);
  isCleared = signal(false);
  previewUrl = signal<string | null>(null);
  error = signal('');

  shownFileName = computed(() => this.isCleared() ? null : this.fileName());

  hasPicture = computed(() => this.previewUrl() !== null || this.shownFileName() !== null);

  /**
   * Shows the picked file without sending it.
   *
   * The input is cleared either way, because picking the same file twice in a
   * row raises no change event otherwise. The size is checked here only to say
   * which file and why: the API refuses an oversized body with a bare 413.
   *
   * @param event The file input's change event.
   */
  choose = (event: Event): void => {
    const input = event.target as HTMLInputElement;
    const image = input.files?.[0];

    input.value = '';

    if (!image) {
      return;
    }

    if (image.size > this.maxBytes) {
      this.error.set('That picture is larger than 500KB. Pick a smaller one.');
      return;
    }

    this.dropPreview();

    this.error.set('');
    this.picked.set(image);
    this.isCleared.set(false);
    this.previewUrl.set(URL.createObjectURL(image));
  }

  /**
   * Marks the picture for removal, which the form acts on when it saves.
   */
  remove = (): void => {
    this.dropPreview();
    this.picked.set(null);
    this.isCleared.set(true);
  }

  /**
   * Releases the preview. An object URL pins its file in memory for the life
   * of the tab until it is revoked, so every one that is replaced or abandoned
   * goes through here.
   */
  dropPreview(): void {
    const url = this.previewUrl();

    if (url) {
      URL.revokeObjectURL(url);
    }

    this.previewUrl.set(null);
  }

  /**
   * Releases the preview when the form goes, so a picture that was picked and
   * never saved does not stay pinned in memory.
   */
  ngOnDestroy(): void {
    this.dropPreview();
  }
}
