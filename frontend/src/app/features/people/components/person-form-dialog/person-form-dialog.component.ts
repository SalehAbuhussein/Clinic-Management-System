import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogRef, MatDialogTitle } from '@angular/material/dialog';
import { MatButton } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatError, MatFormField, MatHint, MatLabel } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatInput } from '@angular/material/input';
import { MatOption, MatSelect } from '@angular/material/select';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { disabled, form, FormField, maxLength, readonly, required } from '@angular/forms/signals';
import { formatDate } from '@angular/common';

import { PersonService } from '@shared/services/person/person.service';

import { ApiError } from '@core/auth/types/auth.types';
import { Person } from '@shared/services/person/person.types';
import { PersonFormData } from '@features/people/types/people.types';

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
    MatError,
    MatFormField,
    MatHint,
    MatIcon,
    MatInput,
    MatLabel,
    MatOption,
    MatProgressSpinner,
    MatSelect,
  ],
  selector: 'app-person-form-dialog',
  styleUrl: './person-form-dialog.component.scss',
  templateUrl: './person-form-dialog.component.html',
})
export class PersonFormDialogComponent {
  dialogRef = inject(MatDialogRef<PersonFormDialogComponent>);
  personService = inject(PersonService);
  snackbar = inject(MatSnackBar);
  person = inject<Person>(MAT_DIALOG_DATA);

  isSaving = signal(false);
  error = signal('');

  personModel = signal<PersonFormData>({
    firstname: this.person.firstname,
    midname: this.person.midname,
    thirdname: this.person.thirdname ?? '',
    lastname: this.person.lastname,
    nationalNo: this.person.nationalNo,
    phone: this.person.phone,
    gender: this.person.gender,
    dateOfBirth: new Date(`${this.person.dateOfBirth}T00:00:00`),
  });

  personForm = form(this.personModel, (schema) => {
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
  });

  /**
   * Sends the form, then closes truthy so the screen reloads its list.
   */
  savePerson = (event: Event): void => {
    event.preventDefault();

    if (this.personForm().invalid()) {
      this.personForm().markAsTouched();
      return;
    }

    if (this.isSaving()) {
      return;
    }

    this.isSaving.set(true);
    this.error.set('');

    const value = this.personForm().value();

    this.personService.updatePerson(this.person.id, {
      firstname: value.firstname.trim(),
      midname: value.midname.trim(),
      thirdname: value.thirdname.trim() || undefined,
      lastname: value.lastname.trim(),
      phone: value.phone.trim(),
      gender: value.gender!,
      dateOfBirth: formatDate(value.dateOfBirth!, 'yyyy-MM-dd', 'en-US'),
    }).subscribe({
      next: () => {
        this.snackbar.open('Person updated', 'Dismiss', { duration: 5000 });
        this.dialogRef.close(true);
      },
      error: (error: ApiError) => {
        this.isSaving.set(false);
        this.error.set(error.message);
      },
    });
  }
}
