import { Component, inject, signal } from '@angular/core';
import { MatAnchor, MatButton } from '@angular/material/button';
import { MatDatepickerToggle, MatDatepicker, MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialogActions, MatDialogClose, MatDialogContent, MatDialogRef, MatDialogTitle } from '@angular/material/dialog';
import { MatError, MatFormField, MatHint, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { MatSelect, MatOption } from '@angular/material/select';
import { MatSlideToggle } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { form, FormField, maxLength, minLength, readonly, required, validate } from '@angular/forms/signals';

import { AuthService } from '@core/auth/service/auth.service';

import { ApiError } from '@core/auth/types/auth.types';
import { CreateUserRequest } from '@shared/services/user/user.types';

type ProfileData = Required<CreateUserRequest> & { currentPassword: string, newPassword: string; confirmPassword: string; };

@Component({
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
  snackbar = inject(MatSnackBar);

  profileModel = signal<ProfileData>({
    firstname: '',
    midname: '',
    thirdname: '',
    lastname: '',
    dateOfBirth: '',
    nationalNo: '',
    gender: false,
    phone: '',
    email: '',
    password: '',
    currentPassword: '',
    newPassword: '',
    confirmPassword: ''
  });
  profileForm = form(this.profileModel, (schema) => {
    required(schema.firstname),
    maxLength(schema.firstname, 20),

    required(schema.midname),
    maxLength(schema.midname, 20),

    maxLength(schema.thirdname, 20),

    required(schema.lastname),
    maxLength(schema.lastname, 20),

    required(schema.nationalNo),
    maxLength(schema.nationalNo, 15),

    required(schema.phone),
    maxLength(schema.phone, 15),

    required(schema.gender),

    readonly(schema.dateOfBirth),

    required(schema.currentPassword, { when: () => this.showUserInfo() });

    required(schema.password, { when: () => this.showUserInfo() });
    minLength(schema.password, 6, { when: () => this.showUserInfo() });

    required(schema.confirmPassword, { when: () => this.showUserInfo() });
    validate(schema.confirmPassword, ({ valueOf }) => valueOf(schema.confirmPassword) === valueOf(schema.password)
      ? null
      : { kind: 'mismatch', message: 'The two passwords do not match' });
  });

  showUserInfo = signal(false);
  isSaving = signal<boolean>(false);
  error = signal('');

  /**
   * Changes the signed-in user's own password, which is why the current one is
   * required. Nothing else on this dialog is saved: the API has no endpoint
   * that updates a person.
   */
  changePassword = (): void => {
    if (this.profileForm().invalid()) {
      this.profileForm().markAsTouched();
      return;
    }

    if (this.isSaving()) {
      return;
    }

    const userId = this.authService.userId();

    if (userId === null) {
      this.error.set('Sign in again to change your password.');
      return;
    }

    this.isSaving.set(true);
    this.error.set('');

    const { currentPassword, password } = this.profileForm().value();

    this.authService.changePassword(userId, { currentPassword, newPassword: password }).subscribe({
      next: () => {
        this.snackbar.open('Password changed', 'Dismiss', { duration: 5000 });
        this.dialogRef.close(true);
      },
      error: (error: ApiError) => {
        this.isSaving.set(false);
        this.error.set(error.message);
      },
    });
  }
}
