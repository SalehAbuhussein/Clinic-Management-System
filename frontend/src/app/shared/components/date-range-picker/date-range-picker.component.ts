import { ChangeDetectionStrategy, Component, model } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButton } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormField } from '@angular/material/form-field';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, MatButton, MatDatepickerModule, MatFormField],
  selector: 'app-date-range-picker',
  styleUrl: './date-range-picker.component.scss',
  templateUrl: './date-range-picker.component.html',
})
export class DateRangePickerComponent {
  from = model.required<Date>();
  to = model.required<Date>();

  /**
   * Both ends land on today, or on a window ending today.
   *
   * @param days How many days the window covers, counting today.
   */
  selectLastDays(days: number): void {
    const to = new Date();
    const from = new Date(to.getFullYear(), to.getMonth(), to.getDate() - (days - 1));

    this.from.set(from);
    this.to.set(to);
  }

  /**
   * The range input nulls the end between the two clicks that set it, so a null
   * is a half-finished pick rather than a cleared range.
   */
  setFrom(value: Date | null): void {
    if (value) {
      this.from.set(value);
    }
  }

  setTo(value: Date | null): void {
    if (value) {
      this.to.set(value);
    }
  }
}
