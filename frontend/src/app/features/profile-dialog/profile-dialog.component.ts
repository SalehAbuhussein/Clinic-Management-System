import { Component, signal } from '@angular/core';
import { MatAnchor } from '@angular/material/button';
import { MatDatepickerToggle, MatDatepicker, MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialogActions, MatDialogClose, MatDialogContent, MatDialogTitle } from '@angular/material/dialog';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { MatSelect, MatOption } from '@angular/material/select';
import { MatSlideToggle } from '@angular/material/slide-toggle';
import { form, FormField, maxLength, minLength, readonly, required } from '@angular/forms/signals';

import { CreateUserRequest } from '@shared/services/user/user.types';

type ProfileData = Required<CreateUserRequest> & { currentPassword: string, newPassword: string; confirmPassword: string; };

@Component({
  imports: [
    FormField,
    MatAnchor,
    MatDatepicker,
    MatDatepickerModule,
    MatDatepickerToggle,
    MatDialogActions,
    MatDialogClose,
    MatDialogContent,
    MatDialogTitle,
    MatFormField,
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

    minLength(schema.password, 6)
  });

  showUserInfo = signal(false);
  isSaving = signal<boolean>(false);
}
