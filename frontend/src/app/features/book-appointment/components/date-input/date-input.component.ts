import { ChangeDetectionStrategy, Component, inject, model } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormField, MatLabel, MatSuffix } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';

import { WorkingDaysService } from '@shared/services/working-days/working-days.service';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, MatDatepickerModule, MatFormField, MatInput, MatLabel, MatSuffix],
  selector: 'app-date-input',
  styleUrl: './date-input.component.scss',
  templateUrl: './date-input.component.html',
})
export class DateInputComponent {
  workingDaysService = inject(WorkingDaysService);

  date = model<Date | null>(null);

  /**
   * Greys out the days the clinic is closed. An arrow property rather than a
   * method, because the datepicker keeps the reference it was given and a
   * method would be a new binding on every check.
   *
   * It opens every day until the list loads, so a slow request shows nothing
   * rather than a calendar with no selectable days at all.
   */
  isOpenOn = (date: Date | null): boolean => date === null || this.workingDaysService.isWorkingDay(date);
}
