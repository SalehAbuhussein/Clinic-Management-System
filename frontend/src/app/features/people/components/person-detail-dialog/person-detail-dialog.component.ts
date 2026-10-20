import { Component, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogTitle } from '@angular/material/dialog';
import { MatButton } from '@angular/material/button';

import { Person } from '@shared/services/person/person.types';

@Component({
  imports: [DatePipe, MatButton, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogTitle],
  selector: 'app-person-detail-dialog',
  styleUrl: './person-detail-dialog.component.scss',
  templateUrl: './person-detail-dialog.component.html',
})
export class PersonDetailDialogComponent {
  person = inject<Person>(MAT_DIALOG_DATA);
}
