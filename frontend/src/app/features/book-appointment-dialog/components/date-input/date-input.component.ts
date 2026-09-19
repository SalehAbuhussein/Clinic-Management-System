import { Component, model } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormField, MatLabel, MatSuffix } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';

@Component({
  imports: [FormsModule, MatDatepickerModule, MatFormField, MatInput, MatLabel, MatSuffix],
  selector: 'app-date-input',
  styleUrl: './date-input.component.scss',
  templateUrl: './date-input.component.html',
})
export class DateInputComponent {
  date = model<Date | null>(null);
}
