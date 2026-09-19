import { Component, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButton } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialogContent, MatDialogTitle, MatDialogActions, MatDialogClose } from '@angular/material/dialog';
import { MatFormField, MatLabel, MatHint } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';

import { DateInputComponent } from './components/date-input/date-input.component';
import { DoctorSelectorInputComponent } from './components/doctor-selector-input/doctor-selector-input.component';
import { PatientSelectorInputComponent } from './components/patient-selector-input/patient-selector-input.component';

@Component({
  imports: [
    DateInputComponent,
    DoctorSelectorInputComponent,
    FormsModule,
    MatChipsModule,
    MatDialogContent,
    MatDialogTitle,
    MatFormField,
    MatInput,
    MatLabel,
    PatientSelectorInputComponent,
    MatHint,
    MatDialogActions,
    MatButton,
    MatDialogClose
],
  selector: 'app-book-appointment-dialog',
  styleUrl: './book-appointment-dialog.component.scss',
  templateUrl: './book-appointment-dialog.component.html',
})
export class BookAppointmentDialogComponent {
  readonly DURATIONS = [15, 30, 45, 60];

  date = signal<Date | null>(new Date());
  time = signal("09:30");
  minutes = signal(30);
  notes = signal('');

  canBook = computed(() => !!this.date() && !!this.time());
}
