import { ChangeDetectionStrategy, Component, computed, inject, OnDestroy, signal } from '@angular/core';
import { MatAnchor, MatButton } from '@angular/material/button';
import { MatDatepickerToggle, MatDatepicker, MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialogActions, MatDialogClose, MatDialogContent, MatDialogRef, MatDialogTitle } from '@angular/material/dialog';
import { MatError, MatFormField, MatHint, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { MatSelect, MatOption } from '@angular/material/select';
import { MatSlideToggle } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { disabled, form, FormField, maxLength, minLength, readonly, required, validate } from '@angular/forms/signals';
import { formatDate } from '@angular/common';

import { Observable, of, switchMap } from 'rxjs';

import { AuthService } from '@core/auth/service/auth.service';
import { ProfileService } from '@shared/services/profile/profile.service';

import { ApiError } from '@core/auth/types/auth.types';
import { ProfileData } from '@features/profile/types/profile.types';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormField,
    MatAnchor,
    MatButton,
    MatDatepicker,
    MatDatepickerModule,
    MatDatepickerToggle,
    MatDialogActions,
    MatDialogClose,
    MatDialogContent,
    MatDialogTitle,
    MatError,
    MatFormField,
    MatHint,
    MatInput,
    MatLabel,
    MatOption,
    MatProgressSpinner,
    MatSelect,
    MatSlideToggle,
  ],
  selector: 'app-profile-dialog',
  styleUrl: './profile-dialog.component.scss',
  templateUrl: './profile-dialog.component.html',
})
export class ProfileDialogComponent implements OnDestroy {
  authService = inject(AuthService);
  dialogRef = inject(MatDialogRef<ProfileDialogComponent>);
  profileService = inject(ProfileService);
  snackbar = inject(MatSnackBar);

  isLoading = signal(true);
  isSaving = signal(false);
  showUserInfo = signal(false);
  error = signal('');

  maxImageBytes = 2 * 1024 * 1024; // 2MB limit

  imageFileName = signal<string | null>(null);
  pickedImage = signal<File | null>(null);
  isImageCleared = signal(false);
  previewUrl = signal<string | null>(null);

  avatarUrl = computed(() => {
    if (this.previewUrl()) {
      return this.previewUrl();
    }

    return this.isImageCleared() ? null : this.profileService.imageUrlOf(this.imageFileName());
  });

  avatarInitials = computed(() => {
    const { firstname, lastname } = this.profileModel();

    return `${firstname.charAt(0)}${lastname.charAt(0)}`.toUpperCase();
  });

  profileModel = signal<ProfileData>({
    firstname: '',
    midname: '',
    thirdname: '',
    lastname: '',
    nationalNo: '',
    phone: '',
    gender: null,
    dateOfBirth: null,
    currentPassword: '',
    password: '',
    confirmPassword: '',
  });

  profileForm = form(this.profileModel, (schema) => {
    required(schema.firstname);
    maxLength(schema.firstname, 20);

    required(schema.midname);
    maxLength(schema.midname, 20);

    maxLength(schema.thirdname, 20);

    required(schema.lastname);
    maxLength(schema.lastname, 20);

    disabled(schema.nationalNo);

    required(schema.phone);
    maxLength(schema.phone, 15);

    required(schema.gender);

    required(schema.dateOfBirth);
    readonly(schema.dateOfBirth);

    required(schema.currentPassword, { when: () => this.showUserInfo() });

    required(schema.password, { when: () => this.showUserInfo() });
    minLength(schema.password, 6, { when: () => this.showUserInfo() });

    required(schema.confirmPassword, { when: () => this.showUserInfo() });
    validate(schema.confirmPassword, ({ valueOf }) => valueOf(schema.confirmPassword) === valueOf(schema.password)
      ? null
      : { kind: 'mismatch', message: 'The two passwords do not match' });
  });

  constructor() {
    this.loadProfile();
  }

  /**
   * Reads the signed-in user's own record.
   */
  loadProfile(): void {
    const userId = this.currentUserId();

    if (!userId) {
      this.isLoading.set(false);
      return;
    }

    this.profileService.getProfile(userId).subscribe({
      next: (profile) => {
        this.profileModel.update((model) => ({
          ...model,
          firstname: profile.firstname,
          midname: profile.midname,
          thirdname: profile.thirdname ?? '',
          lastname: profile.lastname,
          nationalNo: profile.nationalNo,
          phone: profile.phone,
          gender: profile.gender,
          dateOfBirth: new Date(`${profile.dateOfBirth}T00:00:00`),
        }));

        this.imageFileName.set(profile.profileImage);

        this.isLoading.set(false);
      },
      error: (error: ApiError) => {
        this.isLoading.set(false);
        this.error.set(error.message);
      },
    });
  }

  /**
   * Saves the person half. The password has its own call, because the API keeps
   * them apart and a failed password should not discard a corrected phone number.
   */
  saveProfile = (): void => {
    if (!this.canSubmit()) {
      return;
    }

    const userId = this.currentUserId();

    if (!userId) {
      return;
    }

    const value = this.profileForm().value();

    this.run(
      this.profileService.updateProfile(userId, {
        firstname: value.firstname.trim(),
        midname: value.midname.trim(),
        thirdname: value.thirdname.trim() || undefined,
        lastname: value.lastname.trim(),
        phone: value.phone.trim(),
        gender: value.gender!,
        dateOfBirth: formatDate(value.dateOfBirth!, 'yyyy-MM-dd', 'en-US'),
      }).pipe(switchMap(() => this.imageRequest(userId))),
      'Profile saved',
    );
  }

  /**
   * Changes the caller's own password, which is why the current one is required.
   */
  changePassword = (): void => {
    if (!this.canSubmit()) {
      return;
    }

    const userId = this.currentUserId();

    if (userId === null) {
      return;
    }

    const { currentPassword, password } = this.profileForm().value();

    this.run(
      this.authService.changePassword(userId, { currentPassword, newPassword: password }),
      'Password changed',
    );
  }

  /**
   * Shows the picked file without sending it. Nothing reaches the server until
   * Save, so trying five pictures still uploads one.
   *
   * The input is cleared either way, because picking the same file twice in a
   * row raises no change event otherwise. The size is checked here only to say
   * which file and why: the API refuses an oversized body with a bare 413.
   *
   * @param event The file input's change event.
   */
  chooseImage = (event: Event): void => {
    const input = event.target as HTMLInputElement;
    const image = input.files?.[0];

    input.value = '';

    if (!image) {
      return;
    }

    if (image.size > this.maxImageBytes) {
      this.error.set('That picture is larger than 2 MB. Pick a smaller one.');
      return;
    }

    this.dropPreview();

    this.error.set('');
    this.pickedImage.set(image);
    this.isImageCleared.set(false);
    this.previewUrl.set(URL.createObjectURL(image));
  }

  /**
   * Marks the picture for removal. The stored file survives until Save, so
   * closing the dialog leaves it alone.
   */
  removeImage = (): void => {
    this.dropPreview();
    this.pickedImage.set(null);
    this.isImageCleared.set(true);
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
   * The picture call the save should carry, if any.
   *
   * @param userId The account.
   * @returns The upload, the removal, or nothing to do.
   */
  imageRequest(userId: number): Observable<unknown> {
    const image = this.pickedImage();

    if (image) {
      return this.profileService.uploadProfileImage(userId, image);
    }

    if (this.isImageCleared() && this.imageFileName()) {
      return this.profileService.removeProfileImage(userId);
    }

    return of(null);
  }

  /**
   * Both saves name the account in the route, so both need the id the token
   * carries.
   *
   * @returns The signed-in user's id, or null once the token has gone.
   */
  currentUserId(): number | null {
    const userId = this.authService.userId();

    if (!userId) {
      this.error.set('Sign in again to continue.');
    }

    return userId;
  }

  /**
   * @returns Whether the form is worth sending, marking it touched if not.
   */
  canSubmit(): boolean {
    if (this.profileForm().invalid()) {
      this.profileForm().markAsTouched();
      return false;
    }

    return !this.isSaving();
  }

  /**
   * Runs one of the two saves and closes on success.
   *
   * @param request The call to make.
   * @param message What to say when it succeeds.
   */
  run(request: Observable<unknown>, message: string): void {
    this.isSaving.set(true);
    this.error.set('');

    request.subscribe({
      next: () => {
        this.snackbar.open(message, 'Dismiss', { duration: 5000 });
        this.dialogRef.close(true);
      },
      error: (error: ApiError) => {
        this.isSaving.set(false);
        this.error.set(error.message);
      },
    });
  }

  /**
   * Releases the preview when the dialog goes, so a picture that was picked
   * and never saved does not stay pinned in memory.
   */
  ngOnDestroy(): void {
    this.dropPreview();
  }
}
