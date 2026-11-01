import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { form, FormField, maxLength, minLength, required } from '@angular/forms/signals';
import { MAT_DIALOG_DATA, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogRef, MatDialogTitle } from '@angular/material/dialog';
import { MatButton } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormField, MatHint, MatLabel, MatSuffix } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatInput } from '@angular/material/input';
import { MatOption, MatSelect } from '@angular/material/select';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { formatDate } from '@angular/common';

import { Observable } from 'rxjs';

import { UserService } from '@shared/services/user/user.service';

import { ApiError } from '@core/auth/types/auth.types';
import { User } from '@shared/services/user/user.types';
import { UserFormValue } from '@features/users/types/users.types';

/**
 * Creates an account, or edits the two things an existing one can change.
 *
 * The person behind a user is written once and never from here, so editing
 * shows only the email and the role and the rest of the schema stands down.
 */
@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormField,
    MatButton,
    MatDatepickerModule,
    MatDialogActions,
    MatDialogClose,
    MatDialogContent,
    MatDialogTitle,
    MatFormField,
    MatHint,
    MatIcon,
    MatInput,
    MatLabel,
    MatOption,
    MatProgressSpinner,
    MatSelect,
    MatSuffix,
  ],
  selector: 'app-user-form-dialog',
  styleUrl: './user-form-dialog.component.scss',
  templateUrl: './user-form-dialog.component.html',
})
export class UserFormDialogComponent {
  dialogRef = inject(MatDialogRef<UserFormDialogComponent>);
  snackbar = inject(MatSnackBar);
  userService = inject(UserService);
  existingUser = inject<User | null>(MAT_DIALOG_DATA, { optional: true });

  readonly isEditing = !!this.existingUser;

  model = signal<UserFormValue>({
    firstname: '',
    midname: '',
    thirdname: '',
    lastname: '',
    nationalNo: '',
    phone: '',
    gender: null,
    dateOfBirth: null,
    email: this.existingUser?.email ?? '',
    password: '',
    role: this.existingUser?.role ?? 'Staff',
  });

  userForm = form(this.model, (user) => {
    required(user.email);
    maxLength(user.email, 255);

    maxLength(user.firstname, 20);
    maxLength(user.midname, 20);
    maxLength(user.thirdname, 20);
    maxLength(user.lastname, 20);
    maxLength(user.nationalNo, 15);
    maxLength(user.phone, 15);
    maxLength(user.password, 255);

    if (!this.isEditing) {
      required(user.firstname);
      required(user.midname);
      required(user.lastname);
      required(user.nationalNo);
      required(user.phone);
      required(user.gender);
      required(user.dateOfBirth);

      required(user.password);
      minLength(user.password, 6);
    }
  });

  isSaving = signal(false);
  error = signal('');

  canSave = computed(() => this.userForm().valid());

  /**
   * Submits the form. Which call it makes is the only thing the mode decides.
   */
  saveUser = () => {
    if (!this.canSave() || this.isSaving()) {
      return;
    }

    this.isSaving.set(true);
    this.error.set('');

    const existing = this.existingUser;
    const value = this.model();

    const request: Observable<unknown> = existing
      ? this.userService.updateUser(existing.id, {
        email: value.email.trim(),
        role: value.role === existing.role ? undefined : value.role,
      })
      : this.userService.createUser({
        firstname: value.firstname.trim(),
        midname: value.midname.trim(),
        thirdname: value.thirdname.trim() || undefined,
        lastname: value.lastname.trim(),
        nationalNo: value.nationalNo.trim(),
        phone: value.phone.trim(),
        gender: value.gender!,
        dateOfBirth: formatDate(value.dateOfBirth!, 'yyyy-MM-dd', 'en-US'),
        email: value.email.trim(),
        password: value.password,
      });

    request.subscribe({
      next: () => {
        this.snackbar.open(this.isEditing ? 'User updated' : 'User created', 'Dismiss', { duration: 5000 });
        this.dialogRef.close(true);
      },
      error: (error: ApiError) => {
        this.isSaving.set(false);
        this.error.set(error.message);
      },
    });
  }
}
