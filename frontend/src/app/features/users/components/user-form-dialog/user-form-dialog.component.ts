import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
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

import { ApiError, Role } from '@core/auth/types/auth.types';
import { User } from '@shared/services/user/user.types';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
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
export class UserFormDialogComponent implements OnInit {
  dialogRef = inject(MatDialogRef<UserFormDialogComponent>);
  snackbar = inject(MatSnackBar);
  userService = inject(UserService);
  existingUser = inject<User | null>(MAT_DIALOG_DATA, { optional: true });

  readonly isEditing = !!this.existingUser;

  firstname = signal('');
  midname = signal('');
  thirdname = signal('');
  lastname = signal('');
  nationalNo = signal('');
  phone = signal('');
  gender = signal<boolean | null>(null);
  dateOfBirth = signal<Date | null>(null);
  password = signal('');
  email = signal('');
  role = signal<Role>('Staff');

  isSaving = signal(false);
  error = signal('');

  canSave = computed(() => {
    if (!this.email().trim()) {
      return false;
    }

    if (this.isEditing) {
      return true;
    }

    return !!this.firstname().trim()
      && !!this.midname().trim()
      && !!this.lastname().trim()
      && !!this.nationalNo().trim()
      && !!this.phone().trim()
      && this.gender() !== null
      && !!this.dateOfBirth()
      && this.password().length >= 6;
  });

  /**
   * Angular onInit lifecycle method
   */
  ngOnInit(): void {
    this.initComponent();
  }

  /**
   * Everything the dialog needs before its first render.
   */
  initComponent(): void {
    this.prefillFromExisting();
  }

  /**
   * Fills the form from the user being edited. Creating starts empty.
   */
  prefillFromExisting(): void {
    const existing = this.existingUser;

    if (!existing) {
      return;
    }

    this.email.set(existing.email);
    this.role.set(existing.role);
  }

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

    const request: Observable<unknown> = existing
      ? this.userService.updateUser(existing.id, {
        email: this.email().trim(),
        role: this.role() === existing.role ? undefined : this.role(),
      })
      : this.userService.createUser({
        firstname: this.firstname().trim(),
        midname: this.midname().trim(),
        thirdname: this.thirdname().trim() || undefined,
        lastname: this.lastname().trim(),
        nationalNo: this.nationalNo().trim(),
        phone: this.phone().trim(),
        gender: this.gender()!,
        dateOfBirth: formatDate(this.dateOfBirth()!, "yyyy-MM-dd", 'en-US'),
        email: this.email().trim(),
        password: this.password(),
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
