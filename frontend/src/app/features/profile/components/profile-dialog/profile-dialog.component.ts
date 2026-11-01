import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
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

import { Observable } from 'rxjs';

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
export class ProfileDialogComponent {
  authService = inject(AuthService);
  dialogRef = inject(MatDialogRef<ProfileDialogComponent>);
  profileService = inject(ProfileService);
  snackbar = inject(MatSnackBar);

  isLoading = signal(true);
  isSaving = signal(false);
  isSavingImage = signal(false);
  showUserInfo = signal(false);
  error = signal('');

  maxImageBytes = 2 * 1024 * 1024;

  imageFileName = signal<string | null>(null);

  imageUrl = computed(() => this.profileService.imageUrlOf(this.imageFileName()));

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
      }),
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
   * Sends the picked file and keeps the name the API stored it under.
   *
   * The input is cleared either way, so picking the same file again still
   * raises a change event. The size is checked here only to say something
   * useful: the API refuses an oversized body with a bare 413.
   *
   * @param event The file input's change event.
   */
  chooseImage = (event: Event): void => {
    const input = event.target as HTMLInputElement;
    const image = input.files?.[0];

    input.value = '';

    const userId = this.currentUserId();

    if (!image || !userId) {
      return;
    }

    if (image.size > this.maxImageBytes) {
      this.error.set('That picture is larger than 2 MB. Pick a smaller one.');
      return;
    }

    this.isSavingImage.set(true);
    this.error.set('');

    this.profileService.uploadProfileImage(userId, image).subscribe({
      next: (fileName) => {
        this.imageFileName.set(fileName);
        this.isSavingImage.set(false);
        this.snackbar.open('Picture updated', 'Dismiss', { duration: 4000 });
      },
      error: (error: ApiError) => {
        this.isSavingImage.set(false);
        this.error.set(error.message);
      },
    });
  }

  /**
   * Clears the picture. The stored file goes with it, so there is nothing to
   * undo and the button is worth a confirm only if that ever stops being cheap.
   */
  removeImage = (): void => {
    const userId = this.currentUserId();

    if (!userId) {
      return;
    }

    this.isSavingImage.set(true);
    this.error.set('');

    this.profileService.removeProfileImage(userId).subscribe({
      next: () => {
        this.imageFileName.set(null);
        this.isSavingImage.set(false);
      },
      error: (error: ApiError) => {
        this.isSavingImage.set(false);
        this.error.set(error.message);
      },
    });
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
}
